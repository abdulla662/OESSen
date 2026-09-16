using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Numerics;

namespace OES.Blazor.Components.Common
{
    public partial class DualRangeSlider<T> : ComponentBase where T : struct, INumber<T>
    {
        [Parameter] public T Min { get; set; } = T.Zero;
        [Parameter] public T Max { get; set; } = T.CreateChecked(100);
        [Parameter] public T Step { get; set; } = T.One;
        [Parameter] public T MinDistance { get; set; } = T.Zero;
        [Parameter] public bool PushOther { get; set; } = true;
        [Parameter] public T MinValue { get; set; } = T.CreateChecked(25);
        [Parameter] public EventCallback<T> MinValueChanged { get; set; }
        [Parameter] public T MaxValue { get; set; } = T.CreateChecked(75);
        [Parameter] public EventCallback<T> MaxValueChanged { get; set; }
        [Parameter] public EventCallback<(T Min, T Max)> RangeChanged { get; set; }
        [Parameter] public bool ShowPerSliderValues { get; set; } = true;
        [Parameter] public bool ShowBand { get; set; } = true;
        [Parameter] public bool ShowRangeSummary { get; set; } = true;
        [Parameter] public string? ValueFormat { get; set; } = "0.00";
        [Parameter] public IFormatProvider? FormatProvider { get; set; } = CultureInfo.InvariantCulture;
        [Parameter] public int TrackHeightPx { get; set; } = 12;

        private T MinValueInternal
        {
            get => _minValue;
            set
            {
                if (_updating) { _minValue = value; return; }

                var v = Clamp(value, Min, Max);

                if (v > _maxValue - MinDistance)
                {
                    if (PushOther)
                    {
                        _updating = true;
                        _maxValue = Clamp(v + MinDistance, Min, Max);
                        _updating = false;
                    }
                    v = T.Min(v, _maxValue - MinDistance);
                }

                if (!v.Equals(_minValue))
                {
                    _minValue = v;
                    _ = EmitChangesAsync();
                }
            }
        }

        private T MaxValueInternal
        {
            get => _maxValue;
            set
            {
                if (_updating) { _maxValue = value; return; }

                var v = Clamp(value, Min, Max);

                if (v < _minValue + MinDistance)
                {
                    if (PushOther)
                    {
                        _updating = true;
                        _minValue = Clamp(v - MinDistance, Min, Max);
                        _updating = false;
                    }
                    v = T.Max(v, _minValue + MinDistance);
                }

                if (!v.Equals(_maxValue))
                {
                    _maxValue = v;
                    _ = EmitChangesAsync();
                }
            }
        }

        private string BandVars
        {
            get
            {
                double range = Math.Max(double.CreateChecked(Max - Min), 1e-9);
                double startPct = (double.CreateChecked(_minValue - Min) / range) * 100.0;
                double endPct = (double.CreateChecked(_maxValue - Min) / range) * 100.0;
                double sizePct = Math.Max(endPct - startPct, 0);

                var startStr = startPct.ToString("F4", CultureInfo.InvariantCulture);
                var sizeStr = sizePct.ToString("F4", CultureInfo.InvariantCulture);

                return $"--start:{startStr}%; --size:{sizeStr}%;";
            }
        }

        private T _minValue;
        private T _maxValue;
        private bool _updating;


        protected override void OnParametersSet()
        {
            if (_updating) return;

            _minValue = Clamp(MinValue, Min, Max);
            _maxValue = Clamp(MaxValue, Min, Max);

            if (_minValue > _maxValue - MinDistance)
                _minValue = T.Max(Min, _maxValue - MinDistance);
        }

        private async Task EmitChangesAsync()
        {
            _updating = true;
            if (MinValueChanged.HasDelegate) await MinValueChanged.InvokeAsync(_minValue);
            if (MaxValueChanged.HasDelegate) await MaxValueChanged.InvokeAsync(_maxValue);
            if (RangeChanged.HasDelegate) await RangeChanged.InvokeAsync((_minValue, _maxValue));
            _updating = false;
            StateHasChanged();
        }

        private static T Clamp(T v, T min, T max) => T.Min(T.Max(v, min), max);

        private string FormatValue(T v)
            => v is IFormattable f ? f.ToString(ValueFormat, FormatProvider) : v.ToString() ?? string.Empty;
    }
}
