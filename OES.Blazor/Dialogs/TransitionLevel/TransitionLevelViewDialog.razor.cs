using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.Paper.Transition;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using System.Globalization;

namespace OES.Blazor.Dialogs.TransitionLevel
{
    public partial class TransitionLevelViewDialog
    {
        [Inject] IBlazTransitionLevelService BlazTransitionLevel { get; set; }

        [Parameter] public long TransitionLevelId { get; set; }

        private GetTransitionLevelDto Model { get; set; } = new GetTransitionLevelDto();
        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
        private bool IsInitialized { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Model = await BlazTransitionLevel.GetTransitionLevelById(TransitionLevelId);

            Model ??= new GetTransitionLevelDto();

            IsInitialized = true;
        }
    }
}
