using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Numerics;

namespace OES.Blazor.Components.Common;
public partial class DualRangeSingleSlider<T> : ComponentBase where T : struct, INumber<T>
{
    [Parameter] public T Min { get; set; } = T.Zero;
    [Parameter] public T Max { get; set; } = T.CreateChecked(1);
    [Parameter] public T Step { get; set; } = T.CreateChecked(0.01);
    [Parameter] public T MinDistance { get; set; } = T.Zero;
    [Parameter] public bool PushOther { get; set; } = true;
    [Parameter] public T MinValue { get; set; }
    [Parameter] public EventCallback<T> MinValueChanged { get; set; }
    [Parameter] public T MaxValue { get; set; }
    [Parameter] public EventCallback<T> MaxValueChanged { get; set; }
    [Parameter] public EventCallback<(T Min, T Max)> RangeChanged { get; set; }
    [Parameter] public bool ShowPerSliderValues { get; set; } = true;
    [Parameter] public bool ShowBand { get; set; } = true;
    [Parameter] public bool ShowRangeSummary { get; set; } = true;
    [Parameter] public int TrackHeightPx { get; set; } = 12;
    [Parameter] public string ValueFormat { get; set; } = "F2";
    [Parameter] public IFormatProvider FormatProvider { get; set; } = CultureInfo.CurrentCulture;

    private T MinValueInternal
    {
        get => MinValue;
        set
        {
            if (_updating) return;
            _updating = true;

            var clamped = Clamp(value, Min, Max);
            if (clamped > MaxValue - MinDistance)
            {
                if (PushOther) MaxValueInternal = clamped + MinDistance;
                clamped = MaxValue - MinDistance;
            }

            if (!Equals(clamped, MinValue))
            {
                MinValue = clamped;
                MinValueChanged.InvokeAsync(clamped);
                RangeChanged.InvokeAsync((clamped, MaxValue));
            }
            _updating = false;
        }
    }

    private T MaxValueInternal
    {
        get => MaxValue;
        set
        {
            if (_updating) return;
            _updating = true;

            var clamped = Clamp(value, Min, Max);
            if (clamped < MinValue + MinDistance)
            {
                if (PushOther) MinValueInternal = clamped - MinDistance;
                clamped = MinValue + MinDistance;
            }

            if (!Equals(clamped, MaxValue))
            {
                MaxValue = clamped;
                MaxValueChanged.InvokeAsync(clamped);
                RangeChanged.InvokeAsync((MinValue, clamped));
            }
            _updating = false;
        }
    }

    private double LeftPercent => Percent(MinValue);

    private double RightPercent => Percent(MaxValue);

    private string FillStyle => $"left:{LeftPercent}%; width:{RightPercent - LeftPercent}%;";

    private bool _updating;

    protected override void OnParametersSet()
    {
        MinValue = Clamp(MinValue, Min, Max);
        MaxValue = Clamp(MaxValue, Min, Max);
        if (MinValue > MaxValue) MinValue = MaxValue;
        if (MaxValue < MinValue) MaxValue = MinValue;
    }

    private double Percent(T value)
    {
        var range = Convert.ToDouble(Max - Min);
        return range <= 0 ? 0 : Math.Clamp(Convert.ToDouble(value - Min) / range * 100, 0, 100);
    }

    private string FormatValue(T value)
        => value is IFormattable f ? f.ToString(ValueFormat, FormatProvider) : value.ToString() ?? "";

    private static T Clamp(T value, T min, T max)
    {
        return value < min ? min : value > max ? max : value;
    }

    private static bool Equals(T a, T b) => a == b;
}
