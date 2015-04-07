using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PowerArgs;
using Exception = System.Exception;

namespace LeafToCRERPEImport
{
    internal class Program
    {
        #region Internal Classes

        private struct Counters
        {
            public const string Setup = "Setup";
            public const string TaxRate = "TaxRate";
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

        #endregion

        #region Vars

        private static Dictionary<string, int> _taxMap;
        private static Dictionary<string, string> _printerMap;
        private static Dictionary<string, string> _jobCodeMap;
        private static Dictionary<string, string> _departmentMap;
        private static Dictionary<string, string> _modifierMap;
        private static Dictionary<string, PosModifierGroup> _modifierGroupsMap;
        private static Dictionary<string, PosItem> _itemMap;
        private static Dictionary<string, int> _counters;

        #endregion

        private static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<CommandLineArgs>(args);

                _taxMap = new Dictionary<string, int>();
                _printerMap = new Dictionary<string, string>();
                _jobCodeMap = new Dictionary<string, string>();
                _departmentMap = new Dictionary<string, string>();
                _modifierMap = new Dictionary<string, string>();
                _modifierGroupsMap = new Dictionary<string, PosModifierGroup>();
                _itemMap = new Dictionary<string, PosItem>();
                _counters = new Dictionary<string, int>();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                using (var reader = System.IO.File.OpenText(parsed.LeafExportFullPath))
                {
                    using (var db = new CreModel())
                    {
                        var leafStore =
                            ServiceStack.Text.JsonSerializer.DeserializeFromReader<LeafDataModel.Store>(reader);

                        SetupStore(db, leafStore);
                        SetupTaxes(db, leafStore);
                        //SetupPrinters(db, leafStore);
                        //SetupJobCodes(db, leafStore);
                        //SetupUsers(db, leafStore);
                        //SetupDepartments(db, leafStore);
                        //SetupModifiers(db, leafStore);
                        //SetupModifierGroups(db, leafStore);
                        //SetupItems(db, leafStore);
                        //SetupMenu(db, leafStore);
                    }
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

        #region Helpers

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

        #endregion

        private static void SetupStore(CreModel db, LeafDataModel.Store leafStore)
        {
            var setup = db.Setups.FirstOrDefault();
            if (setup == null)
            {
                setup = new Setup {StoreID = "1001"};
                db.Add(setup);
            }

            setup.CompanyInfo1 = leafStore.siteName;
            setup.CompanyInfo2 = "";
            setup.CompanyInfo3 = "";
            setup.CompanyInfo4 = "";
            setup.CompanyInfo5 = "";
            setup.StoreEmail = leafStore.siteEmail;
            setup.Phone1 = leafStore.sitePhone;
            setup.Address = leafStore.primary_address.address;
            setup.City = leafStore.primary_address.city;
            setup.State = leafStore.primary_address.stateShort;
            setup.ZipCode = leafStore.primary_address.postalCode;

            db.SaveChanges();

            IncrementCounter(Counters.Setup);
            Log("Updated Store");
        }

        private static void SetupTaxes(CreModel db, LeafDataModel.Store leafStore)
        {
            var taxrate = db.TaxRates.FirstOrDefault();
            if (taxrate == null)
            {
                return;
            }

            if (leafStore.salesTax != 0)
            {
                taxrate.Tax1Name = "salesTax";
                taxrate.Tax1Rate = (float?) leafStore.salesTax;
                _taxMap.Add("salesTax", 1);
            }
            if (leafStore.foodTax != 0)
            {
                taxrate.Tax2Name = "foodTax";
                taxrate.Tax2Rate = (float?) leafStore.foodTax;
                _taxMap.Add("foodTax", 2);
            }
            if (leafStore.bevTax != 0)
            {
                taxrate.Tax3Name = "bevTax";
                taxrate.Tax3Rate = (float?) leafStore.bevTax;
                _taxMap.Add("bevTax", 3);
            }

            db.SaveChanges();

            IncrementCounter(Counters.TaxRate);
            Log("Updated Tax Rates");
        }
    }
}
