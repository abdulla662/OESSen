using AutoMapper;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.Enums;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class MarkingSchemeService(ICommonService _commonService, IMapper _mapper, IFormQuestionScoreService _formQuestionScoreService) : IMarkingSchemeService
    {
        public async Task<ApiResponse> GetPaperMarkingSchemeAsync(long paperId)
        {
            var markingScheme = await _commonService
                ._unitOfWork
                .Repository<MarkingScheme, long>()
                .GetObjAsync(x => x.PaperId == paperId);

            if (markingScheme == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.MarkingSchemeNotFound);
            }

            var dto = new MarkingSchemeDtos(
                markingScheme.Id,
                markingScheme.Name,
                markingScheme.ScoreType,
                markingScheme.Data,
                markingScheme.PaperId
            );

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.MarkingSchemeNotFound,
                                dto);
        }

        public async Task<ApiResponse> AddOrUpdateMarkingSchemeAsync(MarkingSchemeDto markingSchemeDto)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == markingSchemeDto.PaperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            if (paper.QuestionSelectionType != QuestionSelectionType.Manual)
            {
                await DeletePaperMarkingSchemeAsync(markingSchemeDto.PaperId);
            }

            if (paper.Type == PaperType.Adaptive)
            {
                return await HandleAdpativePaperMarkingSchemeAsync(paper);
            }

            var existingScheme = await _commonService
                ._unitOfWork
                .Repository<MarkingScheme, long>()
                .GetObjAsync(x => x.PaperId == markingSchemeDto.PaperId);

            if (existingScheme == null)
            {
                var mappedScheme = _mapper.Map<MarkingScheme>(markingSchemeDto);

                await _commonService
                    ._unitOfWork
                    .Repository<MarkingScheme, long>()
                    .AddAsync(mappedScheme);
            }
            else
            {
                existingScheme.Name = markingSchemeDto.Name;
                existingScheme.ScoreType = markingSchemeDto.ScoreType;
                existingScheme.Data = MergeMarkingSchemeData(existingScheme.Data, markingSchemeDto.Data, markingSchemeDto.ScoreType);

                _commonService
                    ._unitOfWork
                    .Repository<MarkingScheme, long>()
                    .Update(existingScheme);
            }

            paper.PaperCreationStatus = PaperCreationStatus.MarkingSchemeEstablished;

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var response = await _formQuestionScoreService.ApplyScoresToPaperFormsAsync(markingSchemeDto.PaperId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.MarkingSchemaAddedWithPaperFormsQuestion
                        );
                }
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailedToConfigurePaperMarkingScheme
                    );
        }

        public async Task<ApiResponse> DeletePaperMarkingSchemeAsync(long paperId)
        {
            var scoringScheme = await _commonService
                ._unitOfWork
                .Repository<MarkingScheme, long>()
                .GetObjAsync(result => result.PaperId == paperId);

            if (scoringScheme == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperScoringSchemeNotFound
                );
            }

            _commonService._unitOfWork.Repository<MarkingScheme, long>().Delete(scoringScheme);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaperScoringSchemeDeletedSuccessfully
                );
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                     CustomCodeStatus.SomethingWentWrong,
                     HttpStatusCode.InternalServerError,
                    Resource.FailedToDeletePaperScoringScheme
                );
            }
        }


        #region HELPER UTILITIES
        private async Task<ApiResponse> HandleAdpativePaperMarkingSchemeAsync(PaperMetadata targetPaper)
        {
            var adaptiveScheme = new MarkingScheme
            {
                PaperId = targetPaper.Id,
                Name = "Adaptive Marking Scheme",
                ScoreType = ScoreSchemaType.AdaptiveDistribution,
                Data = "{}"
            };

            await _commonService._unitOfWork.Repository<MarkingScheme, long>().AddAsync(adaptiveScheme);

            targetPaper.PaperCreationStatus = PaperCreationStatus.MarkingSchemeEstablished;

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.MarkingSchemaAddedWithPaperFormsQuestion
            );
        }

        private static string MergeMarkingSchemeData(string existingDataJson, string incomingDataJson, ScoreSchemaType scoreType)
        {
            if (string.IsNullOrWhiteSpace(existingDataJson) || existingDataJson == "{}")
            {
                return incomingDataJson;
            }

            try
            {
                switch (scoreType)
                {
                    case ScoreSchemaType.EqualDistribution:
                        {
                            var existing = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<EqualDistributionForManualDto>>>(existingDataJson);
                            var incoming = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<EqualDistributionForManualDto>>>(incomingDataJson);

                            if (existing?.Data == null || incoming?.Data == null)
                                return incomingDataJson;

                            var incomingFormIds = incoming.Data.Select(x => x.FormId).ToHashSet();

                            existing.Data.RemoveAll(x => incomingFormIds.Contains(x.FormId));

                            existing.Data.AddRange(incoming.Data);

                            return JsonSerializer.Serialize(existing);
                        }

                    case ScoreSchemaType.DifficultyLevelBasedDistribution:
                        {
                            var existing = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<DifficultyLevelDistributionDto>>>(existingDataJson);
                            var incoming = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<DifficultyLevelDistributionDto>>>(incomingDataJson);

                            if (existing?.Data == null || incoming?.Data == null)
                                return incomingDataJson;

                            var incomingFormIds = incoming.Data.Select(x => x.FormId).ToHashSet();

                            existing.Data.RemoveAll(x => incomingFormIds.Contains(x.FormId));

                            existing.Data.AddRange(incoming.Data);

                            return JsonSerializer.Serialize(existing);
                        }

                    case ScoreSchemaType.ItemBankBasedDistribution:
                        {
                            var existing = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<ItemBankDistributionDto>>>(existingDataJson);
                            var incoming = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<ItemBankDistributionDto>>>(incomingDataJson);

                            if (existing?.Data == null || incoming?.Data == null)
                                return incomingDataJson;

                            var incomingFormIds = incoming.Data.Select(x => x.FormId).ToHashSet();

                            existing.Data.RemoveAll(x => incomingFormIds.Contains(x.FormId));

                            existing.Data.AddRange(incoming.Data);

                            return JsonSerializer.Serialize(existing);
                        }

                    default:
                        return incomingDataJson;
                }
            }
            catch
            {
                return incomingDataJson;
            }
        }

        public sealed record MarkingSchemeDtos(long Id, string Name, ScoreSchemaType ScoreType, string Data, long PaperId);
        #endregion HELPER UTILITIES
    }
}