using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Paper.Requests;

namespace OES.Blazor.Dialogs.Paper.Sectioning.ManualSectioning
{
    public partial class ManualSectionPropertiesDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public SectionRequestDto Section { get; set; }

        [Parameter] public List<int> AvailableOrderNumbers { get; set; } = [];

        private double SectionTimeInMinutes { get; set; }

        private double SectionTimeInSeconds { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Section?.IsRestrictedTime == true && Section.TimeInMinutes > 0)
            {
                SectionTimeInMinutes = Math.Floor(Section.TimeInMinutes);
                SectionTimeInSeconds = Math.Round((Section.TimeInMinutes - SectionTimeInMinutes) * 60, 2);
            }
        }

        void OnIsRestrictedInTimeChanged(bool isRestrictedInTime)
        {
            Section.IsRestrictedTime = isRestrictedInTime;

            if (!isRestrictedInTime)
            {
                Section.TimeInMinutes = 0;
            }
        }

        void Save()
        {
            if (Section.IsRestrictedTime)
            {
                Section.TimeInMinutes = SectionTimeInMinutes + (SectionTimeInSeconds / 60);
            }

            MudDialog?.Close(true);
        }

        void Cancel()
        {
            MudDialog?.Cancel();
        }
    }
}
