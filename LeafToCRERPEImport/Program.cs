using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PowerArgs;

namespace LeafToCRERPEImport
{
    class Program
    {
        private struct Counters
        {
            public const string TaxRate = "TaxRate";
            public const string TaxGroup = "TaxGroup";
            public const string TaxGroupTaxRate = "TaxGroupTaxRate";
            public const string Tender = "Tender";
            public const string PaymentProfileTender = "PaymentProfileTender";
            public const string KitchenPrinter = "KitchenPrinter";
            public const string Jobcode = "Jobcode";
            public const string Employee = "Employee";
            public const string EmployeeEmail = "EmployeeEmail";
            public const string EmployeePhone = "EmployeePhone";
            public const string EmployeeJobcode = "EmployeeJobcode";
            public const string Department = "Department";
            public const string Modifier = "Modifier";
            public const string ModifierGroup = "ModifierGroup";
            public const string ModifierGroupMember = "ModifierGroupMember";
            public const string Item = "Item";
            public const string ItemModifierGroup = "ItemModifierGroup";
            public const string KitchenPrinterItemMapping = "KitchenPrinterItemMapping";
            public const string MenuPanel = "MenuPanel";
            public const string MenuButton = "MenuButton";
        }

        private class Mapping
        {
            public string LeafId;
            public string PosId;
        }

        private class ModGroupMapping
        {
            public string LeafId;
            public PosModifierGroup PosModifierGroup;
        }

        private class ItemMapping
        {
            public string LeafId;
            public PosItem PosItem;
        }

        private class PosModifierGroup
        {
            
        }

        private class PosItem
        {
            
        }

        public class CommandLineArgs
        {
            [ArgRequired]
            public string LeafExportFullPath { get; set; }

            [ArgDefaultValue("http://localhost:52454")]
            public string ServerUrl { get; set; }

            [ArgDefaultValue("VSw9i0ujqf40Kx")]
            public string ApiKey { get; set; }
        }

        private static Dictionary<string, Guid> _taxMap;
        private static Dictionary<string, Guid> _printerMap;
        private static Dictionary<string, Guid> _jobCodeMap;
        private static Dictionary<string, Guid> _departmentMap;
        private static Dictionary<string, Guid> _modifierMap;
        private static Dictionary<string, PosModifierGroup> _modifierGroupsMap;
        private static Dictionary<string, PosItem> _itemMap;

        private static Dictionary<string, int> _counters;

        static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<CommandLineArgs>(args);

                _taxMap = new Dictionary<string, Guid>();
                _printerMap = new Dictionary<string, Guid>();
                _jobCodeMap = new Dictionary<string, Guid>();
                _departmentMap = new Dictionary<string, Guid>();
                _modifierMap = new Dictionary<string, Guid>();
                _modifierGroupsMap = new Dictionary<string, PosModifierGroup>();
                _itemMap = new Dictionary<string, PosItem>();
                _counters = new Dictionary<string, int>();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                using (var reader = System.IO.File.OpenText(parsed.LeafExportFullPath))
                {
                    var leafStore = ServiceStack.Text.JsonSerializer.DeserializeFromReader<LeafDataModel.Store>(reader);

                    var api = new Api { Apikey = parsed.ApiKey, BaseUri = parsed.ServerUrl };

                    SetupStore(api, leafStore);
                    SetupTaxes(api, leafStore);
                    SetupTenders(api, leafStore);
                    SetupPrinters(api, leafStore);
                    SetupJobCodes(api, leafStore);
                    SetupUsers(api, leafStore);
                    SetupDepartments(api, leafStore);
                    SetupModifiers(api, leafStore);
                    SetupModifierGroups(api, leafStore);
                    SetupItems(api, leafStore);
                    SetupMenu(api, leafStore);
                }

                stopwatch.Stop();

                PrintResults();

                Log("");
                Log("Import complete, duration: {0:%h}h {0:%m}m {0:%s}s", stopwatch.Elapsed);
            }
            catch (ArgException ex)
            {
                Log(ex.Message);
                Log(ArgUsage.GenerateUsageFromTemplate<CommandLineArgs>().ToString());
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private static void IncrementCounter(string counterName)
        {
            if (_counters.ContainsKey(counterName))
            {
                _counters[counterName] = _counters[counterName] + 1;
            }
            else
            {
                _counters[counterName] = 1;
            }
        }

        private static void Log(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        private static void PrintResults()
        {
            var counters = _counters.OrderBy(pair => pair.Key).ToList();

            Log("");
            Log("==============");
            Log("IMPORT RESULTS");
            Log("==============");

            counters.ForEach(pair => Log("{0} : {1}", pair.Key, pair.Value));
        }
    }
}
