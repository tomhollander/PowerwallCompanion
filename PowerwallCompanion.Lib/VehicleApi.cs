using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public class VehicleApi
    {
        private IApiHelper apiHelper;


        public VehicleApi(IPlatformAdapter platformAdapter)
        {
            apiHelper = new ApiHelper(platformAdapter);
        }

        public async Task<Dictionary<string, VehicleData>> GetVehicles()
        {
            var productsResponse = await apiHelper.CallGetApiWithTokenRefresh("/api/1/products");
            var vehicles = new Dictionary<string, VehicleData>();
            foreach (var p in productsResponse["response"].AsArray())
            {
                if (p["vehicle_id"] != null)
                {
                    var v = new VehicleData();
                    v.VehicleId = p["id"].GetValue<long>().ToString();
                    v.VehicleName = p["display_name"].GetValue<string>();
                    v.IsAwake = p["state"].GetValue<string>() == "online";
                    vehicles.Add(v.VehicleId, v);
                }
            }
            return vehicles;
        }

        public async Task UpdateOnlineStatus(Dictionary<string, VehicleData> vehicles)
        {
            Debug.WriteLine(DateTime.Now + ": UpdateOnlineStatus");
            var productsResponse = await apiHelper.CallGetApiWithTokenRefresh("/api/1/products");
            foreach (var p in productsResponse["response"].AsArray())
            {
                if (p["vehicle_id"] != null)
                {
                    var id = p["id"].GetValue<string>();
                    vehicles[id].IsAwake = p["state"].GetValue<string>() == "online";
                }
            }
        }

        public async Task UpdateChargeLevel(VehicleData vehicle)
        {
            Debug.WriteLine(DateTime.Now + ": Getting charge level for " + vehicle.VehicleName);
            try
            {
                var vehicleResponse = await apiHelper.CallGetApiWithTokenRefresh("/api/1/vehicles/" + vehicle.VehicleId.ToString() + "/vehicle_data");
                vehicle.BatteryLevel = vehicleResponse["response"]["charge_state"]["battery_level"].GetValue<int>();
                vehicle.LastUpdated = DateTime.Now;
            }
            catch
            {
                Debug.WriteLine(DateTime.Now + ": Error getting charge status for " + vehicle.VehicleName);
                vehicle.IsAwake = false;
            }

        }
    }
}
