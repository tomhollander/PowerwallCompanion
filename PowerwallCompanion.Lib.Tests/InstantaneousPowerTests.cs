using PowerwallCompanion.Lib.Models;

namespace PowerwallCompanion.Lib.Tests;

[TestClass]
public class InstantaneousPowerTests
{
    [TestMethod]
    public void GridToHomeNoBatteryOrSolar()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 1000,
            BatteryPower = 0,
            GridPower = 1000
        };
        Assert.AreEqual(1000, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void SolarToHomeNoBatteryOrGrid()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 1000,
            SolarPower = 1000,
            BatteryPower = 0,
            GridPower = 0
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(1000, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void BatteryToHomeNoSolarOrGrid()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 1000,
            BatteryPower = 1000,
            SolarPower = 0,
            GridPower = 0
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(1000, instantaneousPower.BatteryToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void SolarAndGridToHomeNoBattery()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 1000,
            SolarPower = 400,
            BatteryPower = 0,
            GridPower = 600
        };
        Assert.AreEqual(600, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(400, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void SolarToHomeAndGrid()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 400,
            SolarPower = 1000,
            BatteryPower = 0,
            GridPower = -600
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(400, instantaneousPower.SolarToHome);
        Assert.AreEqual(600, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void SolarToBatteryAndHome()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 400,
            SolarPower = 1000,
            BatteryPower = -600, // Charging battery
            GridPower = 0
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(400, instantaneousPower.SolarToHome);
        Assert.AreEqual(600, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void SolarToBatteryGridAndHome()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 500,
            SolarPower = 1000,
            BatteryPower = -300, // Charging battery
            GridPower = -200
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(500, instantaneousPower.SolarToHome);
        Assert.AreEqual(300, instantaneousPower.SolarToBattery);
        Assert.AreEqual(200, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void SolarAndBatteryToHome()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 1000,
            SolarPower = 400,
            BatteryPower = 600, // Discharging battery
            GridPower = 0
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(600, instantaneousPower.BatteryToHome);
        Assert.AreEqual(400, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void GridAndBatteryToHome()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 1000,
            SolarPower = 0,
            BatteryPower = 600, // Discharging battery
            GridPower = 400
        };
        Assert.AreEqual(400, instantaneousPower.GridToHome);
        Assert.AreEqual(600, instantaneousPower.BatteryToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }

    public void GridToHomeAndBattery()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 600,
            SolarPower = 0,
            BatteryPower = -400, // Charging battery
            GridPower = 1000
        };
        Assert.AreEqual(600, instantaneousPower.GridToHome);
        Assert.AreEqual(0, instantaneousPower.BatteryToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(0, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(400, instantaneousPower.GridToBattery);
    }

    [TestMethod]
    public void BatteryToGridAndHome()
    {
        var instantaneousPower = new InstantaneousPower
        {
            HomePower = 600,
            SolarPower = 0,
            BatteryPower = 1000, // Discharging battery
            GridPower = -400 // Excess power sent to grid
        };
        Assert.AreEqual(0, instantaneousPower.GridToHome);
        Assert.AreEqual(600, instantaneousPower.BatteryToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToHome);
        Assert.AreEqual(0, instantaneousPower.SolarToGrid);
        Assert.AreEqual(0, instantaneousPower.SolarToBattery);
        Assert.AreEqual(400, instantaneousPower.BatteryToGrid);
        Assert.AreEqual(0, instantaneousPower.GridToBattery);
    }
}
