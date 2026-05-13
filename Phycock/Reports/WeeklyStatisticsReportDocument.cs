using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace Phycock.Reports
{
    /// <summary>
    /// 週次統計レポートPDF。
    /// </summary>
    public class WeeklyStatisticsReportDocument : IDocument
    {
        private readonly WeeklyStatisticsReportModel _model;

        public WeeklyStatisticsReportDocument(WeeklyStatisticsReportModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(8, Unit.Millimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("Noto Sans CJK JP", "Yu Gothic", "Meiryo", "MS Gothic")
                    .FontSize(7));

                page.Header().Element(ComposeHeader);
                page.Content().PaddingTop(5).Column(column =>
                {
                    column.Spacing(5);
                    column.Item().Element(ComposeChart);
                    column.Item().Element(ComposeScheduleStrip);
                    column.Item().Element(ComposeDetailTable);
                    column.Item().Text("注: このPDFは設計確認用のダミーデータです。実装時は HealthRecord / SleepRecord / ScheduleEntry の実データから集計します。")
                        .FontSize(6)
                        .FontColor(Colors.Grey.Darken1);
                });
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.BorderBottom(1).BorderColor(Colors.Blue.Medium).PaddingBottom(5).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(_model.Title).FontSize(16).Bold();
                    column.Item().Text($"対象者: {_model.TargetUserName} / 期間: {_model.StartDate:yyyy/MM/dd} - {_model.EndDate:yyyy/MM/dd}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(145).AlignRight().AlignMiddle().Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(5)
                    .Text("リタリコ確認用サンプル").FontSize(8).FontColor(Colors.Grey.Darken2);
            });
        }

        private void ComposeChart(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(column =>
            {
                column.Item().Text("体調・気分平均 + 睡眠時間").FontSize(9).Bold();
                column.Item().Height(128).Svg(BuildChartSvg()).FitArea();
                column.Item().PaddingTop(2).Row(row =>
                {
                    AddLegend(row, Colors.Blue.Medium, "本睡眠");
                    AddLegend(row, Colors.Teal.Lighten1, "他睡眠");
                    AddLegend(row, Colors.Red.Medium, "体調平均");
                    AddLegend(row, Colors.Amber.Darken1, "気分平均");
                    AddLegend(row, Colors.Amber.Lighten4, "正常睡眠目安 6-8h");
                    row.RelativeItem();
                });
            });
        }

        private static void AddLegend(RowDescriptor row, string color, string label)
        {
            row.AutoItem().PaddingRight(10).Row(item =>
            {
                item.ConstantItem(12).Height(6).AlignMiddle().Background(color);
                item.AutoItem().PaddingLeft(3).Text(label).FontSize(6);
            });
        }

        private void ComposeScheduleStrip(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("通所スケジュール").FontSize(9).Bold();
                column.Item().Row(row =>
                {
                    foreach (var day in _model.Days)
                    {
                        row.RelativeItem().PaddingRight(3).Element(dayContainer =>
                        {
                            dayContainer.MinHeight(58).Border(1).BorderColor(Colors.Grey.Lighten2).Background(GetScheduleDayBackground(day)).Padding(4).Column(dayColumn =>
                            {
                                dayColumn.Item().Text(FormatDate(day.Date)).FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                                if (day.Schedules.Count == 0)
                                {
                                    dayColumn.Item().PaddingTop(3).Element(x => SchedulePill(x, "予定なし", "休養・準備", Colors.Grey.Medium));
                                    return;
                                }

                                foreach (var schedule in day.Schedules)
                                {
                                    dayColumn.Item().PaddingTop(3).Element(x => SchedulePill(
                                        x,
                                        $"{schedule.Session} {schedule.Location}",
                                        $"{schedule.Status} / {schedule.Activity}",
                                        GetStatusColor(schedule.Status)));
                                }
                            });
                        });
                    }
                });
            });
        }

        private static void SchedulePill(IContainer container, string title, string detail, string color)
        {
            container.BorderLeft(3).BorderColor(color).Background(Colors.White).Padding(3).Column(column =>
            {
                column.Item().Text(title).FontSize(6.5f).Bold();
                column.Item().Text(detail).FontSize(5.5f).FontColor(Colors.Grey.Darken2);
            });
        }

        private void ComposeDetailTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.55f);
                    columns.RelativeColumn(0.55f);
                    columns.RelativeColumn(0.65f);
                    columns.RelativeColumn(0.65f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(3.3f);
                    columns.RelativeColumn(2.45f);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "日付");
                    HeaderCell(header, "体調\n平均");
                    HeaderCell(header, "気分\n平均");
                    HeaderCell(header, "本睡眠");
                    HeaderCell(header, "他睡眠");
                    HeaderCell(header, "通所");
                    HeaderCell(header, "体調記録の内訳（RecordTiming別）");
                    HeaderCell(header, "睡眠記録メモ（入力ありのみ）");
                });

                foreach (var day in _model.Days)
                {
                    BodyCell(table).Text(FormatDate(day.Date));
                    BodyCell(table).AlignCenter().Text(FormatScore(day.ConditionAverage));
                    BodyCell(table).AlignCenter().Text(FormatScore(day.FeelingAverage));
                    BodyCell(table).AlignCenter().Text($"{day.NightSleepHours:0.#}h");
                    BodyCell(table).AlignCenter().Text($"{day.OtherSleepHours:0.#}h");
                    BodyCell(table).Text(day.Schedules.Count == 0 ? "予定なし" : string.Join("\n", day.Schedules.Select(x => $"{x.Session} {x.Status}")));
                    BodyCell(table).Element(cell => ComposeHealthRecords(cell, day.HealthRecords));
                    BodyCell(table).Element(cell => ComposeSleepMemos(cell, day.SleepMemos));
                }
            });
        }

        private static void HeaderCell(TableCellDescriptor header, string text)
        {
            header.Cell()
                .Background("#EEF4FF")
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(3)
                .Text(text)
                .FontSize(6.5f)
                .Bold();
        }

        private static IContainer BodyCell(TableDescriptor table)
        {
            return table.Cell()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(3)
                .MinHeight(28);
        }

        private static void ComposeHealthRecords(IContainer container, List<HealthRecordReportEntryModel> records)
        {
            container.Column(column =>
            {
                foreach (var record in records)
                {
                    column.Item().PaddingBottom(2).Text(text =>
                    {
                        text.Span($"{record.RecordTiming}: ").Bold().FontColor(Colors.Grey.Darken2);
                        text.Span($"体調{record.Condition:0.#} / 気分{record.Feeling:0.#} / 症状: {record.Symptoms}");
                        if (!string.IsNullOrWhiteSpace(record.Memo))
                            text.Span($" / メモ: {record.Memo}");
                    });
                }
            });
        }

        private static void ComposeSleepMemos(IContainer container, List<SleepRecordMemoReportEntryModel> memos)
        {
            container.Column(column =>
            {
                foreach (var memo in memos.Where(x => !string.IsNullOrWhiteSpace(x.Memo)))
                {
                    column.Item().PaddingBottom(2).Text(text =>
                    {
                        text.Span($"{memo.SleepType}: ").Bold().FontColor(Colors.Grey.Darken2);
                        text.Span($"{memo.Hours:0.#}h / {memo.Memo}");
                    });
                }
            });
        }

        private string BuildChartSvg()
        {
            const double left = 54;
            const double top = 18;
            const double width = 680;
            const double height = 88;
            const double maxSleep = 10;
            const double minScore = 1;
            const double maxScore = 5;
            var step = width / _model.Days.Count;
            var barWidth = Math.Min(25, step * 0.36);

            var svg = new StringBuilder();
            svg.AppendLine("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 780 132">""");
            svg.AppendLine("""<rect width="780" height="132" fill="#ffffff"/>""");
            svg.AppendLine($"""<rect x="{left}" y="{top}" width="{width}" height="{height}" fill="#ffffff" stroke="#d7dde3" stroke-width="1"/>""");

            var bandTop = SleepY(8);
            var bandBottom = SleepY(6);
            svg.AppendLine($"""<rect x="{left}" y="{bandTop}" width="{width}" height="{bandBottom - bandTop}" fill="#FFF3CD" opacity="0.75" stroke="#F59F00" stroke-dasharray="4 4"/>""");
            svg.AppendLine($"""<text x="{left + 6}" y="{bandTop + 14}" fill="#7A5B00" font-size="8">正常な睡眠時間目安 6-8h</text>""");

            for (var i = 0; i <= 5; i++)
            {
                var y = top + height - height * i / 5;
                svg.AppendLine($"""<line x1="{left}" y1="{y}" x2="{left + width}" y2="{y}" stroke="#edf0f2" stroke-width="1"/>""");
            }

            svg.AppendLine("""<g fill="#5f6b7a" font-size="8">""");
            svg.AppendLine($"""<text x="20" y="{SleepY(0) + 3}">0h</text>""");
            svg.AppendLine($"""<text x="20" y="{SleepY(4) + 3}">4h</text>""");
            svg.AppendLine($"""<text x="20" y="{SleepY(8) + 3}">8h</text>""");
            svg.AppendLine($"""<text x="20" y="{SleepY(10) + 3}">10h</text>""");
            svg.AppendLine($"""<text x="{left + width + 10}" y="{ScoreY(1) + 3}">1</text>""");
            svg.AppendLine($"""<text x="{left + width + 10}" y="{ScoreY(3) + 3}">3</text>""");
            svg.AppendLine($"""<text x="{left + width + 10}" y="{ScoreY(5) + 3}">5</text>""");
            svg.AppendLine("</g>");

            for (var index = 0; index < _model.Days.Count; index++)
            {
                var day = _model.Days[index];
                var center = left + step * index + step / 2;
                var nightHeight = height * day.NightSleepHours / maxSleep;
                var otherHeight = height * day.OtherSleepHours / maxSleep;
                var x = center - barWidth / 2;
                var nightY = top + height - nightHeight;
                var otherY = nightY - otherHeight;
                svg.AppendLine($"""<rect x="{x}" y="{nightY}" width="{barWidth}" height="{nightHeight}" rx="3" fill="#5B7CFA"/>""");
                if (otherHeight > 0)
                    svg.AppendLine($"""<rect x="{x}" y="{otherY}" width="{barWidth}" height="{otherHeight}" rx="3" fill="#8DD7BF"/>""");
                svg.AppendLine($"""<text x="{center}" y="124" fill="#1f2933" font-size="8" text-anchor="middle">{day.Date:M/d}</text>""");
            }

            AppendPolyline(svg, _model.Days.Select((day, index) => (X: left + step * index + step / 2, Y: ScoreY(day.ConditionAverage ?? minScore))), "#E05D44", false);
            AppendPolyline(svg, _model.Days.Select((day, index) => (X: left + step * index + step / 2, Y: ScoreY(day.FeelingAverage ?? minScore))), "#F59F00", true);

            svg.AppendLine("</svg>");
            return svg.ToString();

            double SleepY(double value) => top + height - height * value / maxSleep;
            double ScoreY(double value) => top + height - height * (value - minScore) / (maxScore - minScore);
        }

        private static void AppendPolyline(StringBuilder svg, IEnumerable<(double X, double Y)> points, string color, bool dashed)
        {
            var pointList = points.ToList();
            var polylinePoints = string.Join(" ", pointList.Select(point => $"{point.X.ToString("0.##", CultureInfo.InvariantCulture)},{point.Y.ToString("0.##", CultureInfo.InvariantCulture)}"));
            var dash = dashed ? " stroke-dasharray=\"7 4\"" : "";
            svg.AppendLine($"""<polyline points="{polylinePoints}" fill="none" stroke="{color}" stroke-width="3"{dash} stroke-linejoin="round"/>""");
            foreach (var point in pointList)
                svg.AppendLine($"""<circle cx="{point.X.ToString("0.##", CultureInfo.InvariantCulture)}" cy="{point.Y.ToString("0.##", CultureInfo.InvariantCulture)}" r="4" fill="{color}"/>""");
        }

        private static string FormatDate(DateOnly date) => date.ToString("M/d ddd", CultureInfo.GetCultureInfo("ja-JP"));

        private static string FormatScore(double? value) => value.HasValue ? value.Value.ToString("0.0", CultureInfo.InvariantCulture) : "-";

        private static string GetStatusColor(string status) => status switch
        {
            "通所済み" => Colors.Green.Medium,
            "予定" => Colors.Blue.Medium,
            "遅刻" => Colors.Orange.Medium,
            "早退" => Colors.DeepPurple.Medium,
            "欠席" => Colors.Red.Medium,
            _ => Colors.Grey.Medium
        };

        private static string GetScheduleDayBackground(WeeklyStatisticsDayReportModel day)
        {
            if (day.Schedules.Any(x => x.Location == "在宅")) return "#E8F6EF";
            if (day.Schedules.Count != 0) return "#E7F1FF";
            return Colors.Grey.Lighten4;
        }
    }
}
