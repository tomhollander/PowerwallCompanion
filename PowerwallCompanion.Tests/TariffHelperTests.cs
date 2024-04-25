using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;

namespace PowerwallCompanion.Tests
{
    [TestClass]
    public class TariffHelperTests
    {

        private const string ratePlanJson = @"{
    ""response"": {
        ""code"": ""(edited)"",
        ""name"": ""Test Tariff"",
        ""utility"": ""Acme Energy"",
        ""daily_charges"": [
            {
                ""amount"": 0,
                ""name"": ""Charge""
            }
        ],
        ""demand_charges"": {
            ""ALL"": {
                ""ALL"": 0
            },
            ""Summer"": {},
            ""Winter"": {}
        },
        ""energy_charges"": {
            ""ALL"": {
                ""ALL"": 0
            },
            ""Season3"": {
                ""OFF_PEAK"": 0.01,
                ""ON_PEAK"": 0.01,
                ""PARTIAL_PEAK"": 0.01,
                ""SUPER_OFF_PEAK"": 0.01
            },
            ""Summer"": {
                ""OFF_PEAK"": 0.3,
                ""ON_PEAK"": 0.47,
                ""SUPER_OFF_PEAK"": 0.15
            },
            ""Winter"": {
                ""OFF_PEAK"": 0.3,
                ""ON_PEAK"": 0.35,
                ""SUPER_OFF_PEAK"": 0.08
            }
        },
        ""seasons"": {
            ""Season3"": {
                ""fromDay"": 1,
                ""toDay"": 30,
                ""fromMonth"": 11,
                ""toMonth"": 11,
                ""tou_periods"": {
                    ""OFF_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 21,
                            ""fromMinute"": 0,
                            ""toHour"": 9,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 21,
                            ""fromMinute"": 0,
                            ""toHour"": 4,
                            ""toMinute"": 30
                        }
                    ],
                    ""ON_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 17,
                            ""fromMinute"": 0,
                            ""toHour"": 21,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 17,
                            ""fromMinute"": 0,
                            ""toHour"": 21,
                            ""toMinute"": 0
                        }
                    ],
                    ""PARTIAL_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 9,
                            ""fromMinute"": 0,
                            ""toHour"": 17,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 4,
                            ""fromMinute"": 30,
                            ""toHour"": 5,
                            ""toMinute"": 30
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 13,
                            ""fromMinute"": 30,
                            ""toHour"": 17,
                            ""toMinute"": 0
                        }
                    ],
                    ""SUPER_OFF_PEAK"": [
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 5,
                            ""fromMinute"": 30,
                            ""toHour"": 13,
                            ""toMinute"": 30
                        }
                    ]
                }
            },
            ""Summer"": {
                ""fromDay"": 1,
                ""toDay"": 31,
                ""fromMonth"": 12,
                ""toMonth"": 3,
                ""tou_periods"": {
                    ""OFF_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 6,
                            ""fromMinute"": 0,
                            ""toHour"": 16,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 21,
                            ""fromMinute"": 0,
                            ""toHour"": 0,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 6,
                            ""fromMinute"": 0,
                            ""toHour"": 0,
                            ""toMinute"": 0
                        }
                    ],
                    ""ON_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 16,
                            ""fromMinute"": 0,
                            ""toHour"": 21,
                            ""toMinute"": 0
                        }
                    ],
                    ""SUPER_OFF_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 0,
                            ""fromMinute"": 0,
                            ""toHour"": 6,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 0,
                            ""fromMinute"": 0,
                            ""toHour"": 6,
                            ""toMinute"": 0
                        }
                    ]
                }
            },
            ""Winter"": {
                ""fromDay"": 1,
                ""toDay"": 31,
                ""fromMonth"": 4,
                ""toMonth"": 10,
                ""tou_periods"": {
                    ""OFF_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 6,
                            ""fromMinute"": 0,
                            ""toHour"": 16,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 20,
                            ""fromMinute"": 0,
                            ""toHour"": 0,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 6,
                            ""fromMinute"": 0,
                            ""toHour"": 0,
                            ""toMinute"": 0
                        }
                    ],
                    ""ON_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 16,
                            ""fromMinute"": 0,
                            ""toHour"": 20,
                            ""toMinute"": 0
                        }
                    ],
                    ""SUPER_OFF_PEAK"": [
                        {
                            ""fromDayOfWeek"": 0,
                            ""toDayOfWeek"": 4,
                            ""fromHour"": 0,
                            ""fromMinute"": 0,
                            ""toHour"": 6,
                            ""toMinute"": 0
                        },
                        {
                            ""fromDayOfWeek"": 5,
                            ""toDayOfWeek"": 6,
                            ""fromHour"": 0,
                            ""fromMinute"": 0,
                            ""toHour"": 6,
                            ""toMinute"": 0
                        }
                    ]
                }
            }
        },
        ""sell_tariff"": {
            ""name"": ""EV night saver (edited)"",
            ""utility"": ""AGL"",
            ""daily_charges"": [
                {
                    ""amount"": 0,
                    ""name"": ""Charge""
                }
            ],
            ""demand_charges"": {
                ""ALL"": {
                    ""ALL"": 0
                },
                ""Summer"": {},
                ""Winter"": {}
            },
            ""energy_charges"": {
                ""ALL"": {
                    ""ALL"": 0
                },
                ""Season3"": {
                    ""OFF_PEAK"": 0.01,
                    ""ON_PEAK"": 0.01,
                    ""PARTIAL_PEAK"": 0.01,
                    ""SUPER_OFF_PEAK"": 0.01
                },
                ""Summer"": {
                    ""OFF_PEAK"": 0.07,
                    ""ON_PEAK"": 0.07,
                    ""SUPER_OFF_PEAK"": 0.07
                },
                ""Winter"": {
                    ""OFF_PEAK"": 0.07,
                    ""ON_PEAK"": 0.07,
                    ""SUPER_OFF_PEAK"": 0.07
                }
            },
            ""seasons"": {
                ""Season3"": {
                    ""fromDay"": 1,
                    ""toDay"": 30,
                    ""fromMonth"": 11,
                    ""toMonth"": 11,
                    ""tou_periods"": {
                        ""OFF_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 21,
                                ""fromMinute"": 0,
                                ""toHour"": 9,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 21,
                                ""fromMinute"": 0,
                                ""toHour"": 4,
                                ""toMinute"": 30
                            }
                        ],
                        ""ON_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 17,
                                ""fromMinute"": 0,
                                ""toHour"": 21,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 17,
                                ""fromMinute"": 0,
                                ""toHour"": 21,
                                ""toMinute"": 0
                            }
                        ],
                        ""PARTIAL_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 9,
                                ""fromMinute"": 0,
                                ""toHour"": 17,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 4,
                                ""fromMinute"": 30,
                                ""toHour"": 5,
                                ""toMinute"": 30
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 13,
                                ""fromMinute"": 30,
                                ""toHour"": 17,
                                ""toMinute"": 0
                            }
                        ],
                        ""SUPER_OFF_PEAK"": [
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 5,
                                ""fromMinute"": 30,
                                ""toHour"": 13,
                                ""toMinute"": 30
                            }
                        ]
                    }
                },
                ""Summer"": {
                    ""fromDay"": 1,
                    ""toDay"": 31,
                    ""fromMonth"": 12,
                    ""toMonth"": 3,
                    ""tou_periods"": {
                        ""OFF_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 6,
                                ""fromMinute"": 0,
                                ""toHour"": 16,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 21,
                                ""fromMinute"": 0,
                                ""toHour"": 0,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 6,
                                ""fromMinute"": 0,
                                ""toHour"": 0,
                                ""toMinute"": 0
                            }
                        ],
                        ""ON_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 16,
                                ""fromMinute"": 0,
                                ""toHour"": 21,
                                ""toMinute"": 0
                            }
                        ],
                        ""SUPER_OFF_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 0,
                                ""fromMinute"": 0,
                                ""toHour"": 6,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 0,
                                ""fromMinute"": 0,
                                ""toHour"": 6,
                                ""toMinute"": 0
                            }
                        ]
                    }
                },
                ""Winter"": {
                    ""fromDay"": 1,
                    ""toDay"": 31,
                    ""fromMonth"": 4,
                    ""toMonth"": 10,
                    ""tou_periods"": {
                        ""OFF_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 6,
                                ""fromMinute"": 0,
                                ""toHour"": 16,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 20,
                                ""fromMinute"": 0,
                                ""toHour"": 0,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 6,
                                ""fromMinute"": 0,
                                ""toHour"": 0,
                                ""toMinute"": 0
                            }
                        ],
                        ""ON_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 16,
                                ""fromMinute"": 0,
                                ""toHour"": 20,
                                ""toMinute"": 0
                            }
                        ],
                        ""SUPER_OFF_PEAK"": [
                            {
                                ""fromDayOfWeek"": 0,
                                ""toDayOfWeek"": 4,
                                ""fromHour"": 0,
                                ""fromMinute"": 0,
                                ""toHour"": 6,
                                ""toMinute"": 0
                            },
                            {
                                ""fromDayOfWeek"": 5,
                                ""toDayOfWeek"": 6,
                                ""fromHour"": 0,
                                ""fromMinute"": 0,
                                ""toHour"": 6,
                                ""toMinute"": 0
                            }
                        ]
                    }
                }
            }
        }
    }
}
";

        [TestMethod]
        public void GetWeekDayTariffsForNonWrappingSeason()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariffs = tariffHelper.GetTariffsForDay(new DateTime(2024, 6, 5, 0, 0, 0));
            Assert.AreEqual(4, tariffs.Count);
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Winter", Name = "SUPER_OFF_PEAK", StartDate = new DateTime(2024, 6, 5, 0, 0, 0), EndDate = new DateTime(2024, 6, 5, 6, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Winter", Name = "OFF_PEAK", StartDate = new DateTime(2024, 6, 5, 6, 0, 0), EndDate = new DateTime(2024, 6, 5, 16, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Winter", Name = "ON_PEAK", StartDate = new DateTime(2024, 6, 5, 16, 0, 0), EndDate = new DateTime(2024, 6, 5, 20, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Winter", Name = "OFF_PEAK", StartDate = new DateTime(2024, 6, 5, 20, 0, 0), EndDate = new DateTime(2024, 6, 6, 0, 0, 0) });
        }

        [TestMethod]
        public void GetWeekendTariffsForNonWrappingSeason()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariffs = tariffHelper.GetTariffsForDay(new DateTime(2024, 6, 1, 0, 0, 0));
            Assert.AreEqual(2, tariffs.Count);
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Winter", Name = "SUPER_OFF_PEAK", StartDate = new DateTime(2024, 6, 1, 0, 0, 0), EndDate = new DateTime(2024, 6, 1, 6, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Winter", Name = "OFF_PEAK", StartDate = new DateTime(2024, 6, 1, 6, 0, 0), EndDate = new DateTime(2024, 6, 2, 0, 0, 0) });        
        }

        [TestMethod]
        public void GetWeekDayTariffsForWrappingSeason()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariffs = tariffHelper.GetTariffsForDay(new DateTime(2024, 2, 5, 0, 0, 0));
            Assert.AreEqual(4, tariffs.Count);
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Summer", Name = "SUPER_OFF_PEAK", StartDate = new DateTime(2024, 2, 5, 0, 0, 0), EndDate = new DateTime(2024, 2, 5, 6, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Summer", Name = "OFF_PEAK", StartDate = new DateTime(2024, 2, 5, 6, 0, 0), EndDate = new DateTime(2024, 2, 5, 16, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Summer", Name = "ON_PEAK", StartDate = new DateTime(2024, 2, 5, 16, 0, 0), EndDate = new DateTime(2024, 2, 5, 21, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Summer", Name = "OFF_PEAK", StartDate = new DateTime(2024, 2, 5, 21, 0, 0), EndDate = new DateTime(2024, 2, 6, 0, 0, 0) });
        }

        [TestMethod]
        public void GetWeekDayTariffsForWrappingDay()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariffs = tariffHelper.GetTariffsForDay(new DateTime(2024, 11, 5, 0, 0, 0));
            Assert.AreEqual(4, tariffs.Count);
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Season3", Name = "OFF_PEAK", StartDate = new DateTime(2024, 11, 5, 0, 0, 0), EndDate = new DateTime(2024, 11, 5, 9, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Season3", Name = "PARTIAL_PEAK", StartDate = new DateTime(2024, 11, 5, 9, 0, 0), EndDate = new DateTime(2024, 11, 5, 17, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Season3", Name = "ON_PEAK", StartDate = new DateTime(2024, 11, 5, 17, 0, 0), EndDate = new DateTime(2024, 11, 5, 21, 0, 0) });
            CollectionAssert.Contains(tariffs, new Tariff { Season = "Season3", Name = "OFF_PEAK", StartDate = new DateTime(2024, 11, 5, 21, 0, 0), EndDate = new DateTime(2024, 11, 6, 0, 0, 0) });
        }

        [TestMethod]
        public void GetInstantTariff()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariff = tariffHelper.GetTariffForInstant(new DateTime(2024, 6, 5, 10, 10, 0));
            Assert.AreEqual("Winter", tariff.Season);
            Assert.AreEqual("OFF_PEAK", tariff.Name);
        }

        [TestMethod]
        public void GetInstantTariffOnBoundary()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariff = tariffHelper.GetTariffForInstant(new DateTime(2024, 6, 5, 16, 0, 0));
            Assert.AreEqual("Winter", tariff.Season);
            Assert.AreEqual("ON_PEAK", tariff.Name);
        }

        [TestMethod]
        public void GetInstantTariffOnWrappingDayMorning()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariff = tariffHelper.GetTariffForInstant(new DateTime(2024, 11, 5, 8, 0, 0));
            Assert.AreEqual("Season3", tariff.Season);
            Assert.AreEqual("OFF_PEAK", tariff.Name);
        }

        [TestMethod]
        public void GetInstantTariffOnWrappingDayEvening()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariff = tariffHelper.GetTariffForInstant(new DateTime(2024, 11, 5, 21, 0, 0));
            Assert.AreEqual("Season3", tariff.Season);
            Assert.AreEqual("OFF_PEAK", tariff.Name);
        }

        [TestMethod]
        public void GetRatesForTariff()
        {
            var tariffHelper = new TariffHelper(JObject.Parse(ratePlanJson));
            var tariff = new Tariff { Season = "Winter", Name = "OFF_PEAK" };
            var rates = tariffHelper.GetRatesForTariff(tariff);
            Assert.AreEqual(0.3m, rates.Item1);
            Assert.AreEqual(0.07m, rates.Item2);
        }
    }
}