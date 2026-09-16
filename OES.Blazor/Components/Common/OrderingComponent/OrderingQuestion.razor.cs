using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;

namespace OES.Blazor.Components.Common.OrderingComponent
{
    public partial class OrderingQuestion
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;
        [Parameter] public bool IsDisabled { get; set; } = true;

        private List<OrderItem> _items = [];

        protected override void OnParametersSet()
        {
            if (Model?.Choices != null && Model.Choices.Count > 0)
            {
                _items = [.. Model.Choices
                    .OrderBy(C => C.OrderId)
                    .Select((c, index) => new OrderItem
                    {
                        Id = c.Id,
                        Text = c.ChoiceText,
                        Order = index
                    })
                ];
            }
        }

        private static void ItemUpdated(MudItemDropInfo<OrderItem> _)
        {
            // Do nothing for now
        }

        public class OrderItem
        {
            public long Id { get; set; }
            public string Text { get; set; }
            public int Order { get; set; }
        }
    }
}
