namespace Phycock.Reports
{
    /// <summary>
    /// リタリコ確認用の週次統計PDFサンプルデータ。
    /// </summary>
    public static class WeeklyStatisticsReportSampleData
    {
        public static WeeklyStatisticsReportModel Create()
        {
            return new WeeklyStatisticsReportModel
            {
                Title = "週次統計レポート サンプル",
                TargetUserName = "サンプル利用者",
                StartDate = new DateOnly(2026, 5, 4),
                EndDate = new DateOnly(2026, 5, 10),
                Days =
                [
                    new()
                    {
                        Date = new DateOnly(2026, 5, 4),
                        ConditionAverage = 4.0,
                        FeelingAverage = 3.8,
                        NightSleepHours = 6.5,
                        OtherSleepHours = 0.5,
                        Schedules =
                        [
                            new() { Session = "AM", Location = "通所", Status = "通所済み", Activity = "ヘルスケア" },
                            new() { Session = "PM", Location = "通所", Status = "通所済み", Activity = "個別訓練" }
                        ],
                        HealthRecords =
                        [
                            new() { RecordTiming = "起床時", Condition = 3, Feeling = 3, Symptoms = "眠気", Memo = "起床直後は重い。" },
                            new() { RecordTiming = "訓練開始時", Condition = 4, Feeling = 4, Symptoms = "なし", Memo = "午後から集中できた。" },
                            new() { RecordTiming = "就眠時", Condition = 5, Feeling = 4, Symptoms = "なし", Memo = "帰宅後も安定。" }
                        ],
                        SleepMemos =
                        [
                            new() { SleepType = "本睡眠", Hours = 6.5, Memo = "途中で一度起きたが再入眠できた。" },
                            new() { SleepType = "他睡眠", Hours = 0.5, Memo = "通所後に短く休んだ。" }
                        ]
                    },
                    new()
                    {
                        Date = new DateOnly(2026, 5, 5),
                        ConditionAverage = 3.2,
                        FeelingAverage = 3.0,
                        NightSleepHours = 5.0,
                        OtherSleepHours = 1.5,
                        Schedules = [new() { Session = "PM", Location = "在宅", Status = "予定", Activity = "セルフワーク" }],
                        HealthRecords =
                        [
                            new() { RecordTiming = "起床時", Condition = 3, Feeling = 2, Symptoms = "頭痛、不安", Memo = "朝から緊張が強い。" },
                            new() { RecordTiming = "就眠時", Condition = 3.5, Feeling = 4, Symptoms = "眠気", Memo = "在宅後は落ち着いた。" }
                        ],
                        SleepMemos =
                        [
                            new() { SleepType = "本睡眠", Hours = 5.0, Memo = "夜間に中途覚醒。" },
                            new() { SleepType = "他睡眠", Hours = 1.5, Memo = "昼寝で補った。" }
                        ]
                    },
                    new()
                    {
                        Date = new DateOnly(2026, 5, 6),
                        ConditionAverage = 3.8,
                        FeelingAverage = 3.4,
                        NightSleepHours = 6.0,
                        OtherSleepHours = 1.0,
                        Schedules =
                        [
                            new() { Session = "AM", Location = "通所", Status = "遅刻", Activity = "職場コミュニケーション" },
                            new() { Session = "PM", Location = "通所", Status = "通所済み", Activity = "部署活動" }
                        ],
                        HealthRecords =
                        [
                            new() { RecordTiming = "起床時", Condition = 3, Feeling = 3, Symptoms = "不安、倦怠感", Memo = "遅刻連絡済み。" },
                            new() { RecordTiming = "訓練開始時", Condition = 4, Feeling = 3.5, Symptoms = "不安", Memo = "作業量を調整。" },
                            new() { RecordTiming = "訓練終了時", Condition = 4.5, Feeling = 4, Symptoms = "なし", Memo = "帰宅前は安定。" }
                        ],
                        SleepMemos =
                        [
                            new() { SleepType = "本睡眠", Hours = 6.0, Memo = "入眠に時間がかかった。" },
                            new() { SleepType = "他睡眠", Hours = 1.0, Memo = "帰宅後に仮眠。" }
                        ]
                    },
                    new()
                    {
                        Date = new DateOnly(2026, 5, 7),
                        ConditionAverage = 2.8,
                        FeelingAverage = 2.6,
                        NightSleepHours = 4.5,
                        OtherSleepHours = 2.0,
                        Schedules = [new() { Session = "AM", Location = "通所", Status = "欠席", Activity = "体調不良" }],
                        HealthRecords =
                        [
                            new() { RecordTiming = "起床時", Condition = 2.5, Feeling = 2, Symptoms = "眠気、頭痛", Memo = "体調不良で休養。" },
                            new() { RecordTiming = "就眠時", Condition = 3, Feeling = 3.2, Symptoms = "眠気", Memo = "少し回復。" }
                        ],
                        SleepMemos =
                        [
                            new() { SleepType = "本睡眠", Hours = 4.5, Memo = "夜の睡眠が短い。" },
                            new() { SleepType = "他睡眠", Hours = 2.0, Memo = "昼寝が長め。" }
                        ]
                    },
                    new()
                    {
                        Date = new DateOnly(2026, 5, 8),
                        ConditionAverage = 4.4,
                        FeelingAverage = 4.2,
                        NightSleepHours = 7.0,
                        OtherSleepHours = 0.5,
                        Schedules = [new() { Session = "AM", Location = "通所", Status = "早退", Activity = "就労前の準備" }],
                        HealthRecords =
                        [
                            new() { RecordTiming = "起床時", Condition = 4.5, Feeling = 4, Symptoms = "なし", Memo = "体調は安定。" },
                            new() { RecordTiming = "訓練終了時", Condition = 4.3, Feeling = 4.4, Symptoms = "軽い倦怠感", Memo = "念のため早退。" }
                        ],
                        SleepMemos = [new() { SleepType = "本睡眠", Hours = 7.0, Memo = "本睡眠が取れて起床後も安定。" }]
                    },
                    new()
                    {
                        Date = new DateOnly(2026, 5, 9),
                        ConditionAverage = 4.1,
                        FeelingAverage = 4.0,
                        NightSleepHours = 7.5,
                        OtherSleepHours = 0,
                        HealthRecords = [new() { RecordTiming = "訓練開始時", Condition = 4.1, Feeling = 4, Symptoms = "なし", Memo = "外出あり。疲労感は軽め。" }],
                        SleepMemos = [new() { SleepType = "本睡眠", Hours = 7.5, Memo = "まとまって眠れた。" }]
                    },
                    new()
                    {
                        Date = new DateOnly(2026, 5, 10),
                        ConditionAverage = 3.6,
                        FeelingAverage = 3.4,
                        NightSleepHours = 6.0,
                        OtherSleepHours = 1.0,
                        HealthRecords = [new() { RecordTiming = "就眠時", Condition = 3.6, Feeling = 3.4, Symptoms = "不安", Memo = "翌週の通所準備で緊張あり。" }],
                        SleepMemos = [new() { SleepType = "他睡眠", Hours = 1.0, Memo = "夕方に短い仮眠。" }]
                    }
                ]
            };
        }
    }
}
