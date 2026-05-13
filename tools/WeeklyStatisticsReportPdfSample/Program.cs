using Phycock.Reports;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var outputPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "週次統計サンプル.pdf"));

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var model = WeeklyStatisticsReportSampleData.Create();
var document = new WeeklyStatisticsReportDocument(model);
document.GeneratePdf(outputPath);

Console.WriteLine(outputPath);
