using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using System.Globalization;
using System.Net;

namespace OES.Blazor.Dialogs.Question.QuestionDetailsDialog
{
    public partial class QuestionDetailsDialog : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }
        [Parameter] public long CreatedQuestionMetaDataId { get; set; }
        [Parameter] public string QuestionTypeParameter { get; set; }
        [Parameter] public QuestionDetailsDto ReceivedQuestionDetailsDto { get; set; } = new(); // NOTE: This represents the main question details, in case of questions that have multiple sub-questions, and not comprehension
        [Parameter] public SegmentQuestionDto ReceivedSegmentQuestionDetailsDto { get; set; } = new();
        [Parameter] public LanguageDto PreSelectedLanguage { get; set; }
        [Parameter] public GetMatchingPairsResponseDto AllMatchingPairsData { get; set; }
        [Parameter] public GetMatchingPairsWithDragDropResponseDto AllMatchingPairsWithDragDropData { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        private const string INSERTION_MODE = nameof(INSERTION_MODE);

        private const string UPDATE_MODE = nameof(UPDATE_MODE);

        private string currentOperationalMode = INSERTION_MODE;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;


        protected override async Task OnInitializedAsync()
        {
            if (ReceivedQuestionDetailsDto.Id == 0)
            {
                currentOperationalMode = INSERTION_MODE;

                ReceivedQuestionDetailsDto.QuestionMetadataId = CreatedQuestionMetaDataId;

                var response = await BlazQuestionService.GetQuestionMetadataByIdAsync(CreatedQuestionMetaDataId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var metadata = (QuestionMetadataRetrievalDto)response.Data;

                    ReceivedQuestionDetailsDto.FileUploadSettings = metadata.FileUploadSettings;
                }
            }
            else
            {
                currentOperationalMode = UPDATE_MODE;
            }
        }

        private async Task HandleQuestionDetailsFormAsync(QuestionDetailsDto questionDetailsDto)
        {
            questionDetailsDto.QuestionMetadataId = CreatedQuestionMetaDataId;

            if (currentOperationalMode == INSERTION_MODE)
            {
                await HandleQuestionDetailsInsertionAsync(questionDetailsDto);
            }
            else
            {
                await HandleQuestionDetailsUpdateAsync(questionDetailsDto);
            }
        }

        private async Task HandleQuestionSegmentDetailsFormAsync(SegmentQuestionDto segmentQuestionDto)
        {
            if (ReceivedSegmentQuestionDetailsDto?.SegmentQuestionDetailsDto?.Id > 0)
            {
                segmentQuestionDto.SegmentQuestionDetailsDto.Id = ReceivedSegmentQuestionDetailsDto.SegmentQuestionDetailsDto.Id;

                if (ReceivedSegmentQuestionDetailsDto.SegmentMetaDataDto != null)
                {
                    segmentQuestionDto.SegmentMetaDataDto.ParentId = ReceivedSegmentQuestionDetailsDto.SegmentMetaDataDto.ParentId;

                    if (string.IsNullOrWhiteSpace(segmentQuestionDto.SegmentMetaDataDto.Code))
                    {
                        segmentQuestionDto.SegmentMetaDataDto.Code = ReceivedSegmentQuestionDetailsDto.SegmentMetaDataDto.Code;
                    }
                }

                if (ReceivedSegmentQuestionDetailsDto.SegmentQuestionConfigDto != null)
                {
                    segmentQuestionDto.SegmentQuestionConfigDto.OrderNumber = ReceivedSegmentQuestionDetailsDto.SegmentQuestionConfigDto.OrderNumber;
                }
            }

            MudDialog.Close(DialogResult.Ok(segmentQuestionDto));
        }

        private async Task HandleQuestionDetailsInsertionAsync(QuestionDetailsDto questionDetailsDto)
        {
            var result = await BlazQuestionService.AddQuestionDetails(questionDetailsDto);

            if (result.CustomCodeStatus == CustomCodeStatus.Success)
            {
                MudDialog.Close(DialogResult.Ok(true));

                Snackbar.Add(result.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private async Task HandleQuestionDetailsUpdateAsync(QuestionDetailsDto questionDetailsDto)
        {
            var result = await BlazQuestionService.EditQuestionDetailsAsync(questionDetailsDto);

            if (result.CustomCodeStatus == CustomCodeStatus.Success)
            {
                MudDialog.Close(DialogResult.Ok(true));

                Snackbar.Add(result.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private async Task HandleMatchingPairsAsync(AddOrUpdateMatchingPairsRequestDto dto)
        {
            var result = await BlazQuestionService.AddOrUpdateMatchingPairsQuestionAsync(dto);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(result.Message, Severity.Success);

                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private async Task HandleMatchingPairsWithDragDropAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto dto)
        {
            var result = await BlazQuestionService.AddOrUpdateMatchingPairsWithDragDropQuestionAsync(dto);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(result.Message, Severity.Success);

                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
        }

        private void CancelDialog() => MudDialog.Cancel();
    }
}
