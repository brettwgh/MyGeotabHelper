using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Logging;

namespace MyGeotabHelper
{
    /// <summary>
    /// MyGeotab Helper methods.
    /// </summary>
    public static class MyGHelpers
    {
        public static ILogger _logger;

        public static void AttachLogger(ILogger logger) => _logger = logger;

        /// <summary>
        /// Gets a list of <see cref="ZoneType"/>s from a list of zone type names.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="zoneTypeNames">A list of <see cref="ZoneType"/> names.</param>
        /// <returns>A list of <see cref="ZoneType"/> entities.</returns>
        public static async Task<List<ZoneType>> GetZoneTypeListByNamesAsync(API api, List<string> zoneTypeNames)
        {
            _logger.LogDebug("Executing GetZoneTypeListByNamesAsync...");

            if (zoneTypeNames is null)
            {
                throw new ArgumentNullException(nameof(zoneTypeNames));
            }
            List<ZoneType> output = new List<ZoneType>();
            foreach (string zoneTypeName in zoneTypeNames)
            {
                List<ZoneType> zoneTypes = await GetZoneTypesByName(api, zoneTypeName);
                output.AddRange(zoneTypes);
            }
            return output;
        }

        /// <summary>
        /// Gets a list of <see cref="Group"/>s by name.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="groupNames">The list of group names.</param>
        /// <returns>A list of <see cref="Group"/> entities.</returns>
        public static async Task<List<Group>> GetGroupListByNamesAsync(API api, List<string> groupNames)
        {
            _logger.LogDebug("Executing GetGroupListByNamesAsync...");

            if (groupNames is null)
            {
                throw new ArgumentNullException(nameof(groupNames));
            }

            List<Group> output = new List<Group>();
            foreach (string groupName in groupNames)
            {
                output.Add(await GetGroupByNameAsync(api, groupName));
            }
            return output;
        }

        /// <summary>
        /// Gets a <see cref="Device"/> by <see cref="Id"/>.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="id">The device id to look for.</param>
        /// <returns>A device if found.</returns>
        public static async Task<Device> GetDeviceByIdAsync(API api, Id id)
        {
            if (api is null)
            {
                throw new ArgumentNullException(nameof(api));
            }

            _logger.LogDebug("Executing GetDeviceByIdAsync...");

            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var search = new Search
            {
                Id = id
            };
            List<Device> devices = await api.CallAsync<List<Device>>("Get", typeof(Device), new { search });
            return devices[0];
        }

        /// <summary>
        /// Adds the <see cref="Group"/> entity list to the <see cref="Device"/>.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/>.</param>
        /// <param name="device">A <see cref="Device"/>.</param>
        /// <param name="groups">A list of <see cref="Group"/> entities.</param>
        /// <returns>An async task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task AddGroupsToDeviceAsync(API api, Device device, List<Group> groups)
        {
            _logger.LogDebug("Executing AddGroupsToDeviceAsync...");

            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (groups is null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            int deviceGroupCountBefore = device.Groups.Count;

            foreach (Group group in groups)
            {
                if (!device.Groups.Contains(group))
                {
                    device.Groups.Add(group);
                    //var output = await api.CallAsync<Device>("Set", typeof(Device), new { entity = device });
                }
            }
            if (deviceGroupCountBefore != device.Groups.Count)
            {
                try
                { 
                    // device group count has changed therefore update 
                    var output = await api.CallAsync<Device>("Set", typeof(Device), new { entity = device });
                }
                catch (GroupRelationViolatedException grve)
                {
                    Console.WriteLine(grve.Message);
                }
            }
        }

        /// <summary>
        /// Removes the <see cref="Group"/> entity list from the <see cref="Device"/>.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/>.</param>
        /// <param name="device">A <see cref="Device"/>.</param>
        /// <param name="groups">A list of <see cref="Group"/> entities.</param>
        /// <returns>An async task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task RemoveGroupsFromDeviceAsync(API api, Device device, List<Group> groups)
        {
            _logger.LogDebug("Executing AddGroupsToDeviceAsync...");

            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (groups is null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            int deviceGroupCountBefore = device.Groups.Count;

            foreach (Group group in groups)
            {
                if (device.Groups.Contains(group))
                {
                    device.Groups.Remove(group);
                }
            }
            if(deviceGroupCountBefore != device.Groups.Count)
            {
                // device group count has changed therefore update 
                var output = await api.CallAsync<Device>("Set", typeof(Device), new { entity = device });
            }
        }

        /// <summary>
        /// Adds a <see cref="Group"/> to a <see cref="Device"/> if the group does not exist.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="device">The device to receive the group.</param>
        /// <param name="group">The group to be added.</param>
        /// <returns>An static async task.</returns>
        public static async Task AddGroupToDeviceAsync(API api, Device device, Group group)
        {
            _logger.LogDebug("Executing AddGroupToDeviceAsync...");

            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (group is null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (!device.Groups.Contains(group))
            {
                IList<Group> existingGroups = device.Groups;
                device.Groups.Add(group);
                var output = await api.CallAsync<Device>("Set", typeof(Device), new { entity = device });
            }
        }

        /// <summary>
        /// Gets a list of <see cref="DeviceStatusInfo"/> objects filtered by the <see cref="Group"/> argument.
        /// Only currently active (non-archived) devices are included.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="group">The <see cref="Group"/> filter argument.</param>
        /// <returns>A list of <see cref="DeviceStatusInfo"/> objects.</returns>
        public static async Task<List<DeviceStatusInfo>> GetDeviceStatusInfoListByGroupAsync(API api, Group group)
        {
            _logger.LogDebug("Executing GetDeviceStatusInfoListByGroupAsync...");

            if (group is null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            var groupSearch = new GroupSearch
            {
                Id = group.Id
            };
            List<GroupSearch> groupSearches = new List<GroupSearch>();
            groupSearches.Add(groupSearch);
            var deviceSearch = new DeviceSearch
            {
                Groups = groupSearches,
                FromDate = DateTime.UtcNow
            };
            DeviceStatusInfoSearch deviceStatusInfoSearch = new DeviceStatusInfoSearch
            {
                DeviceSearch = deviceSearch
            };
            List<DeviceStatusInfo> deviceStatusInfoList = await api.CallAsync<List<DeviceStatusInfo>>("Get", typeof(DeviceStatusInfo), new { search = deviceStatusInfoSearch });
            return deviceStatusInfoList;
        }

        /// <summary>
        /// Gets a List of <see cref="ZoneType"/> objects filtered by name.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="name">The <see cref="ZoneType"/> name.</param>
        /// <returns>A List of <see cref="ZoneType"/> objects.</returns>
        public static async Task<List<ZoneType>> GetZoneTypesByName(API api, string name)
        {
            _logger.LogDebug("Executing GetZoneTypesByName...");

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            List<ZoneType> output = new List<ZoneType>();
            List<ZoneType> zoneTypes = await api.CallAsync<List<ZoneType>>("Get", typeof(ZoneType));
            foreach (ZoneType zoneType in zoneTypes)
            {
                if (zoneType.Name.Equals(name))
                {
                    output.Add(zoneType);
                }
            }
            return output;
        }

        /// <summary>
        /// Gets a List of <see cref="Zone"/> objects filted by <see cref="ZoneType"/>.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="zoneTypes">A List of <see cref="ZoneType"/> objects to filter by.</param>
        /// <returns>A list of <see cref="Zone"/> objects.</returns>
        public static async Task<List<Zone>> GetZonesByZoneType(API api, List<ZoneType> zoneTypes)
        {
            _logger.LogDebug("Executing GetZonesOfZoneType...");

            if (zoneTypes is null)
            {
                throw new ArgumentNullException(nameof(zoneTypes));
            }

            List<Zone> output = new List<Zone>();
            List<Zone> zones = await GetZones(api);

            //output = zones.FindAll(x => x.ZoneTypes.Contains(zoneTypes));
            foreach (Zone zone in zones)
            {
                foreach (ZoneType zoneType in zone.ZoneTypes)
                {
                    if (zoneTypes.Contains(zoneType))
                    {
                        output.Add(zone);
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Gets a list of <see cref="Zone"/> objects.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <returns>A list of <see cref="Zone"/> objects.</returns>
        public static async Task<List<Zone>> GetZones(API api)
        {
            _logger.LogDebug("Executing GetZones...");
            List<Zone> zones = await api.CallAsync<List<Zone>>("Get", typeof(Zone));
            return zones;
        }

        /// <summary>
        /// Gets a <see cref="ReverseGeocodeAddress"/> object from the <see cref="Coordinate"/> objects defined in the <see cref="DeviceStatusInfo"/> object.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="deviceStatusInfo">The <see cref="DeviceStatusInfo"/> object to act on.</param>
        /// <returns>A <see cref="ReverseGeocodeAddress"/> object.</returns>
        public static async Task<ReverseGeocodeAddress> GetAddressesAsync(API api, DeviceStatusInfo deviceStatusInfo)
        {
            _logger.LogDebug("Executing GetAddressesAsync...");

            if (deviceStatusInfo is null)
            {
                throw new ArgumentNullException(nameof(deviceStatusInfo));
            }

            List<Coordinate> coordinates = new List<Coordinate>();
            coordinates.Add(new Coordinate((double)deviceStatusInfo.Longitude, (double)deviceStatusInfo.Latitude));
            List<ReverseGeocodeAddress> addressList = await api.CallAsync<List<ReverseGeocodeAddress>>("GetAddresses", new { coordinates = coordinates });
            return addressList[0];
        }

        /// <summary>
        /// Gets a list of <see cref="Device"/> objects contained in a group.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="group">The <see cref="Group"/> to filter devices by.</param>
        /// <returns>A list of devices related to the group passed in.</returns>
        public static async Task<List<Device>> GetDevicesByGroupAsync(API api, Group group)
        {
            _logger.LogDebug("Executing GetDevicesByGroupAsync...");

            if (group is null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            var groupSearch = new GroupSearch
            {
                Id = group.Id
            };
            List<GroupSearch> groupSearches = new List<GroupSearch>();
            groupSearches.Add(groupSearch);
            var deviceSearch = new DeviceSearch
            {
                Groups = groupSearches,
            };
            List<Device> devices = await api.CallAsync<List<Device>>("Get", typeof(Device), new { search = deviceSearch });
            return devices;
        }

        /// <summary>
        /// Gets the <see cref="Group"/> by the string group name.
        /// </summary>
        /// <param name="api">An initiated instance of the MyGeotab <see cref="API"/> interface.</param>
        /// <param name="name">The textual group name.</param>
        /// <returns>The group if found or null if not.</returns>
        public static async Task<Group> GetGroupByNameAsync(API api, string name)
        {
            _logger.LogDebug("Executing GetGroupByNameAsync...");

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var groupSearch = new GroupSearch
            {
                Name = name,
                IncludeAllTrees = true
            };
            List<Group> groups = await api.CallAsync<List<Group>>("Get", typeof(Group), new { search = groupSearch });
            if (groups.Count == 0)
            {
                return null;
            }
            return groups[0];
        }
    }
}
