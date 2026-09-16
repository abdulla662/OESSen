using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Text.Json;

namespace OES.Blazor.Components.QualityChecker.Comments
{
    public partial class CreateComment
    {
        [Inject] ISnackbar _snackbar { get; set; }
        [Inject] public GlobalUserContext GlobalUserContext { get; set; } = default!;
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IBlazAuthService BlazAuthService { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; } = default!;
        [Inject] IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public long QuestionMetaDataId { get; set; }
        [Parameter] public long ItemBankId { get; set; }
        [Parameter] public EventCallback<long> OnQuestionChanged { get; set; }

        private readonly QCommentDto qCommentDto = new();

        protected override async Task OnParametersSetAsync()
        {
            if (QuestionMetaDataId > 0 && ItemBankId == 0)
            {
                var response = await BlazQuestionService.GetQuestionMetadataByIdAsync(QuestionMetaDataId);
                if (response?.Data is QuestionMetadataRetrievalDto metadata)
                {
                    ItemBankId = (long)metadata.RootItemBankId;
                }
                else if (response?.Data != null)
                {
                    var json = JsonSerializer.Serialize(response.Data);
                    var dto = JsonSerializer.Deserialize<QuestionMetadataRetrievalDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ItemBankId = dto?.RootItemBankId ?? 0;
                }
            }
        }

        private async Task SubmitCommentAsync()
        {
            if (!ValidateInput())
                return;

            var nextId = await BlazQuestionService.GetNextQuestionIdInSameBranchAsync(QuestionMetaDataId);

            qCommentDto.QCGivenStatus = QuestionStatus.Approved;

            qCommentDto.QCAuthor = GlobalUserContext.UserName;

            qCommentDto.QuestionMetaDataId = QuestionMetaDataId;

            var response = await _blazQCommentService.AddComment(qCommentDto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                await BlazSessionStorageService.RemoveValue("PerformEditBtnClick");

                _snackbar.Add(Resource.QuestionApprovedsuccessfully, Severity.Success);

                await GoToNextQuestionWithSameBranch(nextId);
            }
            else
            {
                _snackbar.Add(Resource.pleaseentervalidcomment, Severity.Error);
            }
        }

        private async Task ReturnToEditAsync()
        {
            var authorized = await BlazAuthService.IsCurrentUserAuthorizedForAnyRoleAsync<ItemBankGroupsDto, Guid>(
                ItemBankId,
                [
                    OesTemplateRoleConstants.QuestionEditor,
                    OesTemplateRoleConstants.ItemBankQuestionEditor
                ],
                BlazItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                _snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            if (!ValidateInput())
                return;

            var confirmed = await OpenConfirmationDialogAsync();

            if (!confirmed)
                return;

            qCommentDto.QCGivenStatus = QuestionStatus.ReturnToEdit;

            qCommentDto.QCAuthor = GlobalUserContext.UserName;

            qCommentDto.QuestionMetaDataId = QuestionMetaDataId;

            var response = await _blazQCommentService.AddComment(qCommentDto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                await BlazSessionStorageService.SetValue(
                    nameof(QuestionCreationStatus),
                    (int)QuestionCreationStatus.QuestionDetails);

                await BlazSessionStorageService.SetCrudSessionAsync(
                    QuestionMetaDataId,
                    MiscConstants.PerformEditBtnClick);

                _snackbar.Add(Resource.QuestionReturnedtoEdit, Severity.Success);

                _navigationManager.NavigateTo("/QuestionCreation");
            }
            else
            {
                _snackbar.Add(Resource.pleaseentervalidcomment, Severity.Error);
            }
        }

        private async Task RefuseAsync()
        {
            if (!ValidateInput())
                return;

            var nextId = await BlazQuestionService.GetNextQuestionIdInSameBranchAsync(QuestionMetaDataId);

            qCommentDto.QCGivenStatus = QuestionStatus.Rejected;

            qCommentDto.QCAuthor = GlobalUserContext.UserName;

            qCommentDto.QuestionMetaDataId = QuestionMetaDataId;

            var response = await _blazQCommentService.AddComment(qCommentDto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                await BlazSessionStorageService.RemoveValue("PerformEditBtnClick");

                _snackbar.Add(Resource.QuestionRefused, Severity.Success);

                await GoToNextQuestionWithSameBranch(nextId);
            }
            else
            {
                _snackbar.Add(Resource.pleaseentervalidcomment, Severity.Error);
            }
        }

        private void Cancel()
        {
            _navigationManager.NavigateTo("/QuestionsQualityCheck");
        }

        private async Task LoadQuestionInMiddlePanelAsync()
        {
            var authorized = await BlazAuthService.IsCurrentUserAuthorizedForAnyRoleAsync<ItemBankGroupsDto, Guid>(
                ItemBankId,
                [OesTemplateRoleConstants.QuestionExamViewer],
                BlazItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (!authorized)
            {
                _snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetValue(
                "MiddlePanel_QuestionId",
                QuestionMetaDataId);

            await JSRuntime.InvokeVoidAsync("openInNewTab", "/StaticExam");
        }

        private async Task GoToNextQuestionWithSameBranch(long? nextId)
        {
            if (nextId.HasValue && nextId > 0)
            {
                await OnQuestionChanged.InvokeAsync(nextId.Value);

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                _navigationManager.NavigateTo("/QuestionsQualityCheck");
            }
        }

        private bool ValidateInput()
        {
            if (QuestionMetaDataId <= 0)
            {
                _snackbar.Add(Resource.InvalidQuestionMetadataId, Severity.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(qCommentDto.Comment))
            {
                _snackbar.Add(Resource.pleaseentervalidcomment, Severity.Error);
                return false;
            }

            return true;
        }

        private async Task<bool> OpenConfirmationDialogAsync()
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.ReturnToEdit },
                { p => p.Content, Resource.AreYouSureYouWantToReturnThisQuestionToEdit },
                { p => p.SubmitText, Resource.Yes },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Primary },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Done }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                string.Empty,
                parameters,
                options
            );

            var result = await dialog.Result;

            return !result.Canceled;
        }
    }
}
