using PowerwallCompanion.Lib.Models;

namespace PowerwallCompanion.Lib.Tests;

[TestClass]
public class EnergySiteInfoTests
{
    [TestMethod]
    public void KnownPowerwall2PartNumbersReturnPowerwall2()
    {
        var energySiteInfo = new EnergySiteInfo();
        energySiteInfo.PowerwallPartNumber = "1092170";
        Assert.AreEqual("Powerwall 2", energySiteInfo.PowerwallVersion);

        energySiteInfo.PowerwallPartNumber = "2012170";
        Assert.AreEqual("Powerwall 2", energySiteInfo.PowerwallVersion);

        energySiteInfo.PowerwallPartNumber = "3012170";
        Assert.AreEqual("Powerwall 2", energySiteInfo.PowerwallVersion);

        energySiteInfo.PowerwallPartNumber = "1457844";
        Assert.AreEqual("Powerwall 2", energySiteInfo.PowerwallVersion);

    }

    [TestMethod]
    public void UnknownPowerwallPartNumberReturnsUnknown()
    {
        var energySiteInfo = new EnergySiteInfo();
        energySiteInfo.PowerwallPartNumber = "1234567";
        Assert.AreEqual("Unknown", energySiteInfo.PowerwallVersion);
    }

    [TestMethod]
    public void NullPowerwallPartNumberReturnsUnknown()
    {
        var energySiteInfo = new EnergySiteInfo();
        energySiteInfo.PowerwallPartNumber = null;
        Assert.AreEqual("Unknown", energySiteInfo.PowerwallVersion);
    }
}
