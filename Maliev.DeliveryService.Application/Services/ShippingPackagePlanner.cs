using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Services;

/// <summary>
/// Calculates shipping boxes from analyzed part bounding boxes using first-fit decreasing layered packing.
/// </summary>
public sealed class ShippingPackagePlanner : IShippingPackagePlanner
{
    /// <inheritdoc />
    public IReadOnlyList<ShippingPackageQuoteResponse> PlanPackages(ShippingRateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Parts.Count == 0)
        {
            return
            [
                new ShippingPackageQuoteResponse
                {
                    PackageNumber = 1,
                    Name = request.Parcel.Name,
                    Weight = RoundUp(request.Parcel.Weight),
                    Width = RoundUp(request.Parcel.Width),
                    Length = RoundUp(request.Parcel.Length),
                    Height = RoundUp(request.Parcel.Height),
                    Items =
                    [
                        new ShippingPackageItemResponse
                        {
                            Name = request.Parcel.Name,
                            Quantity = 1,
                            UnitWeight = RoundUp(request.Parcel.Weight),
                            UnitWidth = RoundUp(request.Parcel.Width),
                            UnitLength = RoundUp(request.Parcel.Length),
                            UnitHeight = RoundUp(request.Parcel.Height)
                        }
                    ]
                }
            ];
        }

        var options = PlannerOptions.From(request.Packaging);
        var units = ExpandUnits(request.Parts, options)
            .OrderByDescending(unit => unit.Volume)
            .ThenByDescending(unit => unit.Weight)
            .ToList();
        var boxes = new List<PackageBuilder>();

        foreach (var unit in units)
        {
            var placed = false;
            foreach (var box in boxes.OrderBy(box => box.RemainingWeight(options)).ThenBy(box => box.RemainingVolume(options)))
            {
                if (box.TryAdd(unit, options))
                {
                    placed = true;
                    break;
                }
            }

            if (placed)
            {
                continue;
            }

            var newBox = new PackageBuilder(boxes.Count + 1);
            if (!newBox.TryAdd(unit, options))
            {
                newBox.AddOversized(unit, options);
            }

            boxes.Add(newBox);
        }

        return boxes
            .Select((box, index) => box.ToResponse(index + 1, options))
            .ToList();
    }

    private static IEnumerable<PackedUnit> ExpandUnits(IEnumerable<ShippingPackagePartRequest> parts, PlannerOptions options)
    {
        foreach (var part in parts)
        {
            var margin = Math.Max(0m, part.PackagingMargin ?? options.PartMargin);
            var unit = new PackedUnit(
                FirstNonEmpty(part.Name, "MALIEV part"),
                Math.Max(1m, part.Weight) + options.PartPackagingWeight,
                Math.Max(0.1m, part.Width) + (margin * 2m),
                Math.Max(0.1m, part.Length) + (margin * 2m),
                Math.Max(0.1m, part.Height) + (margin * 2m));

            for (var i = 0; i < Math.Max(1, part.Quantity); i++)
            {
                yield return unit;
            }
        }
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static decimal RoundUp(decimal value) => Math.Ceiling(value * 10m) / 10m;

    private sealed record PlannerOptions(
        decimal PartMargin,
        decimal BoxMargin,
        decimal PartPackagingWeight,
        decimal BoxPackagingWeight,
        decimal MaxWeight,
        decimal MaxLength,
        decimal MaxWidth,
        decimal MaxHeight)
    {
        public decimal InnerLength => Math.Max(0.1m, MaxLength - (BoxMargin * 2m));

        public decimal InnerWidth => Math.Max(0.1m, MaxWidth - (BoxMargin * 2m));

        public decimal InnerHeight => Math.Max(0.1m, MaxHeight - (BoxMargin * 2m));

        public static PlannerOptions From(ShippingPackagingOptionsRequest options) =>
            new(
                Math.Max(0m, options.PartMargin),
                Math.Max(0m, options.BoxMargin),
                Math.Max(0m, options.PartPackagingWeight),
                Math.Max(0m, options.BoxPackagingWeight),
                Math.Max(1m, options.MaxPackageWeight),
                Math.Max(1m, options.MaxPackageLength),
                Math.Max(1m, options.MaxPackageWidth),
                Math.Max(1m, options.MaxPackageHeight));
    }

    private sealed record PackedUnit(string Name, decimal Weight, decimal Width, decimal Length, decimal Height)
    {
        public decimal Volume => Width * Length * Height;

        public IEnumerable<OrientedUnit> Orientations
        {
            get
            {
                var values = new[] { Width, Length, Height };
                return new[]
                    {
                        new OrientedUnit(Name, Weight, values[0], values[1], values[2]),
                        new OrientedUnit(Name, Weight, values[0], values[2], values[1]),
                        new OrientedUnit(Name, Weight, values[1], values[0], values[2]),
                        new OrientedUnit(Name, Weight, values[1], values[2], values[0]),
                        new OrientedUnit(Name, Weight, values[2], values[0], values[1]),
                        new OrientedUnit(Name, Weight, values[2], values[1], values[0])
                    }
                    .Distinct()
                    .OrderBy(unit => unit.Height)
                    .ThenBy(unit => unit.Length * unit.Width);
            }
        }
    }

    private sealed record OrientedUnit(string Name, decimal Weight, decimal Width, decimal Length, decimal Height)
    {
        public decimal Volume => Width * Length * Height;
    }

    private sealed class PackageBuilder
    {
        private readonly List<Layer> _layers = [];
        private readonly Dictionary<string, ShippingPackageItemResponse> _items = new(StringComparer.OrdinalIgnoreCase);

        public PackageBuilder(int packageNumber)
        {
            PackageNumber = packageNumber;
        }

        public int PackageNumber { get; }

        public decimal Weight { get; private set; }

        public bool IsOversized { get; private set; }

        public decimal UsedLength => _layers.Count == 0 ? 0m : _layers.Max(layer => layer.UsedLength);

        public decimal UsedWidth => _layers.Count == 0 ? 0m : _layers.Max(layer => layer.UsedWidth);

        public decimal UsedHeight => _layers.Sum(layer => layer.Height);

        public decimal RemainingWeight(PlannerOptions options) => Math.Max(0m, options.MaxWeight - Weight);

        public decimal RemainingVolume(PlannerOptions options) =>
            Math.Max(0m, (options.InnerLength * options.InnerWidth * options.InnerHeight) - (UsedLength * UsedWidth * UsedHeight));

        public bool TryAdd(PackedUnit unit, PlannerOptions options)
        {
            if (Weight + unit.Weight + options.BoxPackagingWeight > options.MaxWeight)
            {
                return false;
            }

            foreach (var orientation in unit.Orientations)
            {
                if (orientation.Length > options.InnerLength ||
                    orientation.Width > options.InnerWidth ||
                    orientation.Height > options.InnerHeight)
                {
                    continue;
                }

                foreach (var layer in _layers)
                {
                    var otherLayerHeight = UsedHeight - layer.Height;
                    if (layer.TryAdd(orientation, options.InnerLength, options.InnerWidth, options.InnerHeight - otherLayerHeight))
                    {
                        AddItem(orientation);
                        return true;
                    }
                }

                if (UsedHeight + orientation.Height <= options.InnerHeight)
                {
                    var layer = new Layer();
                    if (layer.TryAdd(orientation, options.InnerLength, options.InnerWidth, orientation.Height))
                    {
                        _layers.Add(layer);
                        AddItem(orientation);
                        return true;
                    }
                }
            }

            return false;
        }

        public void AddOversized(PackedUnit unit, PlannerOptions options)
        {
            var orientation = unit.Orientations
                .OrderBy(candidate => Math.Max(0m, candidate.Length - options.InnerLength) +
                    Math.Max(0m, candidate.Width - options.InnerWidth) +
                    Math.Max(0m, candidate.Height - options.InnerHeight))
                .First();

            var layer = new Layer();
            layer.ForceAdd(orientation);
            _layers.Add(layer);
            AddItem(orientation);
            IsOversized = true;
        }

        public ShippingPackageQuoteResponse ToResponse(int packageNumber, PlannerOptions options)
        {
            var width = RoundUp(Math.Max(0.1m, UsedWidth + (options.BoxMargin * 2m)));
            var length = RoundUp(Math.Max(0.1m, UsedLength + (options.BoxMargin * 2m)));
            var height = RoundUp(Math.Max(0.1m, UsedHeight + (options.BoxMargin * 2m)));
            return new ShippingPackageQuoteResponse
            {
                PackageNumber = packageNumber,
                Name = $"MALIEV box {packageNumber}",
                Weight = RoundUp(Weight + options.BoxPackagingWeight),
                Width = width,
                Length = length,
                Height = height,
                IsOversized = IsOversized ||
                    Weight + options.BoxPackagingWeight > options.MaxWeight ||
                    length > options.MaxLength ||
                    width > options.MaxWidth ||
                    height > options.MaxHeight,
                Items = _items.Values.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        private void AddItem(OrientedUnit unit)
        {
            Weight += unit.Weight;
            var key = $"{unit.Name}|{unit.Width}|{unit.Length}|{unit.Height}|{unit.Weight}";
            if (_items.TryGetValue(key, out var existing))
            {
                existing.Quantity++;
                return;
            }

            _items[key] = new ShippingPackageItemResponse
            {
                Name = unit.Name,
                Quantity = 1,
                UnitWeight = RoundUp(unit.Weight),
                UnitWidth = RoundUp(unit.Width),
                UnitLength = RoundUp(unit.Length),
                UnitHeight = RoundUp(unit.Height)
            };
        }
    }

    private sealed class Layer
    {
        private readonly List<Row> _rows = [];

        public decimal Height { get; private set; }

        public decimal UsedWidth => _rows.Sum(row => row.Width);

        public decimal UsedLength => _rows.Count == 0 ? 0m : _rows.Max(row => row.UsedLength);

        public bool TryAdd(OrientedUnit unit, decimal maxLength, decimal maxWidth, decimal maxHeight)
        {
            var newHeight = Math.Max(Height, unit.Height);
            if (newHeight > maxHeight)
            {
                return false;
            }

            foreach (var row in _rows)
            {
                if (unit.Width <= row.Width && row.UsedLength + unit.Length <= maxLength)
                {
                    row.UsedLength += unit.Length;
                    Height = newHeight;
                    return true;
                }
            }

            if (UsedWidth + unit.Width <= maxWidth && unit.Length <= maxLength)
            {
                _rows.Add(new Row(unit.Width, unit.Length));
                Height = newHeight;
                return true;
            }

            return false;
        }

        public void ForceAdd(OrientedUnit unit)
        {
            _rows.Add(new Row(unit.Width, unit.Length));
            Height = unit.Height;
        }
    }

    private sealed class Row(decimal width, decimal usedLength)
    {
        public decimal Width { get; } = width;

        public decimal UsedLength { get; set; } = usedLength;
    }
}
