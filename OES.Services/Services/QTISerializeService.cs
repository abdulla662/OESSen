using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.ExportFiles;
using OES.Helper.Dtos.QTI;
using OES.Helper.General;
using OES.Helper.QTICrypto;
using OES.Interface.Interfaces;
using System.Data;
using System.Text;
using System.Xml.Linq;

namespace OES.Services.Services
{
    public class QTISerializeService(ICommonService _commonService) : IQTISerializeService
    {
        public async Task<byte[]> ExportQuestionsAsQtiFileAsync(ExportDataFileDto exportDataFileDto)
        {
            byte[] byteArray = [];

            try
            {
                var questionMetadataIds = (await _commonService
                                                    ._unitOfWork
                                                    .Repository<QuestionMetadata, long>()
                                                    .GetAllAsync(e => exportDataFileDto.itemBanksIds.Contains(e.ItemBankId) &&
                                                                 e.QuestionStatus == QuestionStatus.Approved))
                                                    .Select(x => x.Id)
                                                    .ToList();

                if (questionMetadataIds.Count == 0)
                {
                    throw new InvalidOperationException("No questions found for the specified item bank.");
                }

                var dto = await GetQtiReverseDtosAsync(questionMetadataIds, exportDataFileDto.languageId, exportDataFileDto.withTags);

                var allQtiXml = SetAssessmentItem(dto);

                byteArray = Encoding.UTF8.GetBytes(allQtiXml);

                return byteArray;
            }
            catch (Exception)
            {
                Console.WriteLine("Error exporting questions as QTI file.");
                return byteArray;
            }
        }


        #region Helper Methods

        private string SetAssessmentItem(List<QtiReverseDto> dto)
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var lineBreakBefore = new XText(Environment.NewLine);

            var Assessment = new XElement(QTIBase.QtiAssessment,
                new XAttribute(XNamespace.Xmlns + "xsi", xsi), // Correctly declare the namespace with Xmlns:xsi
                new XAttribute(xsi + "schemaLocation",
                    "http://www.imsglobal.org/xsd/imsqtiasi_v3p0 https://purl.imsglobal.org/spec/qti/v3p0/schema/xsd/imsqti_asiv3p0p1_v1p0.xsd") // Use xsi namespace for schemaLocation
            );

            foreach (var q in dto)
            {
                // Skip the question if AssessmentIdentifier is not provided
                if (q.AssessmentIdentifier <= 0)
                {
                    continue; // Skip this question
                }

                var assessmentItem = new XElement(QTIBase.QtiAssessmentItem,
                    new XComment("This is the start of the question"),
                    lineBreakBefore,
                    new XAttribute(QTIBase.Identifier, $"{q.AssessmentIdentifier}"),
                    new XAttribute(QTIBase.Title, q.AssessmentCode ?? string.Empty) // Handle null AssessmentCode
                );

                // Add ResponseDeclaration if AssessmentCorrectResponse is not null
                var responseDeclaration = SetResponseDeclaration(q);
                if (responseDeclaration != null)
                {
                    assessmentItem.Add(responseDeclaration);
                }

                // Add ItemBody if AssessmentBody is not null
                var itemBody = SetItemBody(q);
                if (itemBody != null)
                {
                    assessmentItem.Add(itemBody);
                }

                assessmentItem.Add(
                    lineBreakBefore,
                    new XComment("This is the end of the question")
                );

                Assessment.Add(assessmentItem);
            }

            return Assessment.ToString(SaveOptions.None);
        }

        private XElement SetResponseDeclaration(QtiReverseDto dto)
        {
            // Skip if AssessmentCorrectResponse is null or empty
            if (dto.AssessmentCorrectResponse == null || !dto.AssessmentCorrectResponse.Any() || dto.AssessmentCorrectResponse.All(r => string.IsNullOrEmpty(r)))
            {
                return null;
            }

            var baseType = dto.MaxChoices > 0 ? QTIBase.Identifier : "string";

            var assessmentItem = new XElement(QTIBase.QtiResponseDeclaration,
                new XAttribute(QTIBase.BaseType, baseType),
                new XAttribute(QTIBase.Cardinality, QTIBase.Single),
                new XAttribute(QTIBase.Identifier, QTIBase.Response),
                new XElement(QTIBase.QtiCorrectResponse,
                                dto.AssessmentCorrectResponse
                                    .Select(correctAnswer => new XElement(QTIBase.QtiValue, correctAnswer))
                )
            );

            return assessmentItem;
        }

        private XElement SetItemBody(QtiReverseDto dto)
        {
            // Skip if AssessmentBody is null or empty
            if (string.IsNullOrEmpty(dto.AssessmentBody))
            {
                return null;
            }

            var itemBody = new XElement(QTIBase.QtiItemBody,
                new XElement(QTIBase.QtiPrompt, dto.AssessmentBody)
            );

            if (dto.MaxChoices > 0)
            {
                // Add ChoiceInteraction if AssessmentChoices is not null and contains items
                if (dto.AssessmentChoices != null && dto.AssessmentChoices.Any())
                {
                    itemBody.Add(new XElement(QTIBase.QtiChoiceInteraction,
                        new XAttribute(QTIBase.MaxChoices, dto.MaxChoices),
                        new XAttribute(QTIBase.ResponseIdentifier, QTIBase.Response),
                        dto.AssessmentChoices.Select(choice =>
                            new XElement(QTIBase.QtiSimpleChoice,
                                new XAttribute(QTIBase.Identifier, choice.AssessmentChoicesIdentifier),
                                choice.AssessmentChoicesContent ?? string.Empty) // Handle null ChoiceContent
                        )
                    ));
                }
            }
            else
            {
                // Add ExtendedTextInteraction for non-MCQ questions
                itemBody.Add(new XElement(QTIBase.QtiExtendedTextInteraction,
                    new XAttribute(QTIBase.ResponseIdentifier, QTIBase.Response)
                ));
            }

            return itemBody;
        }

        private async Task<List<QuestionMetadata>> GetQuestionsWithDetailsAsync(List<long> questionIds, long languageId)
        {
            return await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .GetAll(q => questionIds.Contains(q.Id) && q.QuestionDetails.Any(qd => qd.LanguageId == languageId))
                    .AsNoTracking()
                    .Include(q => q.QuestionType)
                    .Include(q => q.QuestionDetails.Where(qd => qd.LanguageId == languageId))
                        .ThenInclude(qd => qd.QuestionsChoices)
                    .ToListAsync();
        }

        private async Task<List<QtiReverseDto>> GetQtiReverseDtosAsync(List<long> questionIds, long languageId, bool withTags)
        {
            var questionMetadataList = await GetQuestionsWithDetailsAsync(questionIds, languageId);

            if (questionMetadataList == null || !questionMetadataList.Any())
                return [];

            var dtos = questionMetadataList.ConvertAll(questionMetadata =>
            {
                return questionMetadata.QuestionType.Name switch
                {
                    // For MCQ
                    nameof(OES.Helper.Enums.QuestionType.MCQ)
                    or
                    nameof(OES.Helper.Enums.QuestionType.TrueAndFalse)
                    or
                    nameof(OES.Helper.Enums.QuestionType.MultipleCorrectAnswers)
                    => new QtiReverseDto
                    {
                        AssessmentIdentifier = questionMetadata.Id,
                        AssessmentCode = questionMetadata.Code,
                        AssessmentCorrectResponse = questionMetadata.QuestionDetails
                                                            .SelectMany(qd => qd.QuestionsChoices)
                                                            .Where(c => c.IsCorrectAnswer)
                                                            .Select(c => withTags ? c.ChoiceText : HtmlTagsCleaner.Clean(c.ChoiceText))
                                                            .ToList(),
                        AssessmentBody = withTags ? questionMetadata.QuestionDetails.FirstOrDefault()?.Body
                                                  : HtmlTagsCleaner.Clean(questionMetadata.QuestionDetails.FirstOrDefault()?.Body ?? string.Empty),
                        AssessmentChoices = questionMetadata.QuestionDetails
                            .SelectMany(qd => qd.QuestionsChoices)
                            .Select(c => new AssessmentChoices
                            {
                                AssessmentChoicesIdentifier = c.Id,
                                AssessmentChoicesContent = withTags ? c.ChoiceText : HtmlTagsCleaner.Clean(c.ChoiceText)
                            }).ToList(),
                        MaxChoices = questionMetadata.QuestionDetails.SelectMany(qd => qd.QuestionsChoices).Count(c => c.IsCorrectAnswer)
                    },

                    // For Essay
                    nameof(OES.Helper.Enums.QuestionType.Essay)
                    or
                    nameof(OES.Helper.Enums.QuestionType.Comprehension)
                    => new QtiReverseDto
                    {
                        AssessmentIdentifier = questionMetadata.Id,
                        AssessmentCode = questionMetadata.Code,
                        AssessmentBody = withTags ? questionMetadata.QuestionDetails.FirstOrDefault()?.Body : HtmlTagsCleaner.Clean(questionMetadata.QuestionDetails.FirstOrDefault()?.Body),
                        AssessmentCorrectResponse = [.. questionMetadata.QuestionDetails
                                                        .Select(qd => withTags
                                                            ? qd.ModelAnswer
                                                            : HtmlTagsCleaner.Clean(qd.ModelAnswer ?? string.Empty))
                                                        .Where(modelAnswer => !string.IsNullOrEmpty(modelAnswer))],
                        MaxChoices = 0, // No choices for essay questions
                        AssessmentChoices = []
                    },

                    // For FileUploadResponse
                    nameof(Helper.Enums.QuestionType.FileUploadResponse)
                    => new QtiReverseDto
                    {
                        AssessmentIdentifier = questionMetadata.Id,
                        AssessmentCode = questionMetadata.Code,
                        AssessmentBody = withTags ? questionMetadata.QuestionDetails.FirstOrDefault()?.Body : HtmlTagsCleaner.Clean(questionMetadata.QuestionDetails.FirstOrDefault()?.Body),
                        AssessmentCorrectResponse = [.. questionMetadata.QuestionDetails
                                                        .Select(qd => withTags
                                                            ? qd.ModelAnswer
                                                            : HtmlTagsCleaner.Clean(qd.ModelAnswer ?? string.Empty))
                                                        .Where(modelAnswer => !string.IsNullOrEmpty(modelAnswer))],
                        MaxChoices = 0,
                        AssessmentChoices = []
                    },

                    _ => new QtiReverseDto()
                };
            });

            return dtos;
        }

        #endregion Helper Methods
    }
}