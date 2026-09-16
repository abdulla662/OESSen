using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionVersions;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Enums;

namespace OES.Blazor.Components.QualityChecker.Versions
{
    public partial class QuestionVersionEntry
    {
        [Inject] IBLazQuestionType BLazQuestionType { get; set; } = default!;

        [Parameter, EditorRequired] public QuestionDetailsVersionDto Version { get; set; } = default!;
        [Parameter] public bool IsSub { get; set; }

        private QuestionDataDto Dto { get; set; } = new();
        private Breakpoint SelectedScreenSize { get; set; } = Breakpoint.Md;
        private string SelectScreenSizeClass { get; set; } = "w-100";
        private LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;
        private string QuestionTypeStr { get; set; } = string.Empty;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            var response = await BLazQuestionType.GetQuestionType(Version.QuestionTypeId);

            if (response?.Data is QuestionTypeDto questionType)
            {
                QuestionTypeStr = questionType.Name;
            }

            Dto = new QuestionDataDto
            {
                Id = 0,
                Body = Version.Body,
                ModelAnswer = Version.ModelAnswer,
                Instructions = Version.Instructions,
                LanguageId = Version.LanguageId,
                QuestionMetadataId = Version.QuestionMetadataId,
                Choices = Version.Choices?.Select(c => new ChoiceDataDto
                {
                    Id = c.Id,
                    ChoiceText = c.ChoiceText,
                    IsCorrectAnswer = c.IsCorrectAnswer,
                    HasAttachment = c.HasAttachment,
                    AttachmentFileName = c.AttachmentFileName,
                    OrderId = c.OrderId,
                }).ToList() ?? [],
                HasShuffled = Version.HasShuffled,
                HasAttachment = !string.IsNullOrWhiteSpace(Version.AttachmentFileName),
                AttachmentFileName = Version.AttachmentFileName ?? string.Empty,
                MaxWords = Version.MaxWords,
                MaxRecordingTimeInSeconds = Version.MaxRecordingTimeInSeconds,
                SegmentQuestionConfig = Version.SegmentQuestionPropertiesVersionDto == null ? null : new SegmentQuestionConfigDto
                {
                    HasScore = Version.SegmentQuestionPropertiesVersionDto.HasScore,
                    WordsCount = Version.SegmentQuestionPropertiesVersionDto.WordsCount,
                    ResponseTime = Version.SegmentQuestionPropertiesVersionDto.ResponseTime,
                    SegmentQuestionResponseType = Version.SegmentQuestionPropertiesVersionDto.SegmentQuestionResponseType,
                    ThinkingTime = Version.SegmentQuestionPropertiesVersionDto.ThinkingTime,
                    OrderNumber = Version.SegmentQuestionPropertiesVersionDto.OrderNumber,
                    SegmentAudioUrl = Version.SegmentQuestionPropertiesVersionDto.SegmentAudioUrl
                }
            };
        }
    }
}