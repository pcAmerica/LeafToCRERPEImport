using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dotNetExt;
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

        private class JobCodeMapping
        {
            public string LeafId;
            public JobCode PosJobCode;
        }

        private class ModGroupMapping
        {
            public string LeafId;
            public ModifierGroup PosModifierGroup;
        }

        private class ItemMapping
        {
            public string LeafId;
            public Inventory PosItem;
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
        private static Dictionary<string, JobCode> _jobCodeMap;
        private static Dictionary<string, string> _departmentMap;
        private static Dictionary<string, string> _modifierMap;
        private static Dictionary<string, ModifierGroup> _modifierGroupsMap;
        private static Dictionary<string, Inventory> _itemMap;
        private static Dictionary<string, int> _counters;

        #endregion

        private static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<CommandLineArgs>(args);

                _taxMap = new Dictionary<string, int>();
                _printerMap = new Dictionary<string, string>();
                _jobCodeMap = new Dictionary<string, JobCode>();
                _departmentMap = new Dictionary<string, string>();
                _modifierMap = new Dictionary<string, string>();
                _modifierGroupsMap = new Dictionary<string, ModifierGroup>();
                _itemMap = new Dictionary<string, Inventory>();
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
                        SetupPrinters(db, leafStore);
                        SetupJobCodes(db, leafStore);
                        SetupUsers(db, leafStore);
                        SetupDepartments(db, leafStore);
                        //SetupModifiers(db, leafStore);
                        //SetupModifierGroups(db, leafStore);
                        //SetupItems(db, leafStore);
                        //SetupMenu(db, leafStore);

                        db.SaveChanges();
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

            setup.CompanyInfo1 = leafStore.siteName.Left(30);
            setup.CompanyInfo2 = "";
            setup.CompanyInfo3 = "";
            setup.CompanyInfo4 = "";
            setup.CompanyInfo5 = "";
            setup.StoreEmail = leafStore.siteEmail.Left(50);
            setup.Phone1 = leafStore.sitePhone.Left(15);
            setup.Address = leafStore.primary_address.address.Left(30);
            setup.City = leafStore.primary_address.city.Left(30);
            setup.State = leafStore.primary_address.stateShort.Left(20);
            setup.ZipCode = leafStore.primary_address.postalCode.Left(10);

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

            IncrementCounter(Counters.TaxRate);
            Log("Updated Tax Rates");
        }

        private static void SetupPrinters(CreModel db, LeafDataModel.Store leafStore)
        {
            foreach (var printer in leafStore.printers)
            {
                var map = CreatePrinter(db, printer);
                if (map != null) _printerMap.Add(map.LeafId, map.PosId);
            }
        }

        private static Mapping CreatePrinter(CreModel db, LeafDataModel.Printer leafPrinter)
        {
            var existing = db.FriendlyPrinters.FirstOrDefault(o => o.PrinterName == leafPrinter.printerName);

            if (existing != null)
                return new Mapping {LeafId = leafPrinter.printerName, PosId = existing.PrinterName};

            var printer = new FriendlyPrinter {PrinterName = leafPrinter.printerName.Left(30), StoreID = "1001"};
            db.Add(printer);

            IncrementCounter(Counters.KitchenPrinter);

            Log("Created printer {0}", printer.PrinterName);

            return new Mapping {LeafId = leafPrinter.id, PosId = printer.PrinterName};
        }

        private static void SetupJobCodes(CreModel db, LeafDataModel.Store leafStore)
        {
            foreach (var jobCode in leafStore.job_codes)
            {
                var map = CreateJobCode(db, jobCode);
                if (map != null) _jobCodeMap.Add(map.LeafId, map.PosJobCode);
            }
        }

        private static JobCodeMapping CreateJobCode(CreModel db, LeafDataModel.JobCode leafJobCode)
        {
            var existing = db.JobCodes.FirstOrDefault(o => o.JobCodeID == leafJobCode.id);

            if (existing != null)
                return new JobCodeMapping {LeafId = leafJobCode.id, PosJobCode = existing};

            var jobCode = new JobCode
            {
                JobCodeID = leafJobCode.id.Left(15),
                JobCodeName = leafJobCode.jobCode.Left(15),
                AccessToPos = true,
                DefaultWage = leafJobCode.rate1,
                DefaultOvertimeWage = leafJobCode.otRate1
            };

            db.Add(jobCode);

            var jobCodeStore = new JobCodeStore
            {
                JobCodeID = jobCode.JobCodeID,
                StoreID = "1001"
            };

            db.Add(jobCodeStore);

            IncrementCounter(Counters.Jobcode);

            Log("Created jobcode {0}", jobCode.JobCodeName);

            return new JobCodeMapping {LeafId = leafJobCode.id, PosJobCode = jobCode};
        }

        private static void SetupUsers(CreModel db, LeafDataModel.Store leafStore)
        {
            foreach (var user in leafStore.users)
            {
                CreateUser(db, user);
            }
        }

        private static void CreateUser(CreModel db, LeafDataModel.User leafUser)
        {
            var existing = db.Employees.FirstOrDefault(e => e.CashierID == leafUser.id);

            if (existing != null)
                return;

            var employee = new Employee
            {
                CashierID = leafUser.id,
                EmpName = (leafUser.first + " " + leafUser.last).Left(30),
                FirstName = leafUser.first.Left(15),
                LastName = leafUser.last.Left(20),
                EMail = leafUser.email.Left(50),
                Phone1 = leafUser.phone,
                FormColor = 16777215,
                Password = "b`rghdqA1",
                DispPayOption = true,
                DispItemOption = true,
                OrigStoreID = "1001",
                CreateDate = DateTime.Today,
                PasswordHash = "QSypaQoUu/LW+emLhHO7s+H6tk+MI9/hmqRlX4xSRrY=",
                SaltKey = "vN9A6rY="
            };

            db.Add(employee);

            var employeeStore = new EmployeeStore
            {
                CashierID = employee.CashierID,
                StoreID = "1001"
            };

            db.Add(employeeStore);

            IncrementCounter(Counters.Employee);

            Log("Created employee {0}", employee.EmpName);

            foreach (var leafJobCode in leafUser.job_code_users)
            {
                var jobCode = new EmployeeJobCode
                {
                    CashierID = employee.CashierID,
                    JobCodeID = _jobCodeMap[leafJobCode.job_code_id].JobCodeID,
                    HourlyWage =  _jobCodeMap[leafJobCode.job_code_id].DefaultWage,
                    OvertimeHourlyWage = _jobCodeMap[leafJobCode.job_code_id].DefaultOvertimeWage
                };
                db.Add(jobCode);

                IncrementCounter(Counters.EmployeeJobcode);
            }
        }

        private static void SetupDepartments(CreModel db, LeafDataModel.Store leafStore)
        {
            foreach (var category in leafStore.catalog.categories)
            {
                var map = CreateDepartment(db, category);
                if (map != null) _departmentMap.Add(map.LeafId, map.PosId);
            }
        }

        private static Mapping CreateDepartment(CreModel db, LeafDataModel.Category leafCategory)
        {
            var existing = db.Departments.FirstOrDefault(o => o.DeptID == leafCategory.id);

            if (existing != null)
                return new Mapping {LeafId = leafCategory.id, PosId = existing.DeptID};

            var department = new Department
            {
                StoreID = "1001",
                DeptID = leafCategory.id,
                Description = leafCategory.name.Left(30),
                SubType = "NONE",
                DeptNotes = "",
                CostCalculationPercentage = 0,
                SquareFootage = 0
            };

            if (department.Description.IsNullOrEmpty())
                department.Description = department.DeptID;

            db.Add(department);

            IncrementCounter(Counters.Department);

            Log("Created department {0}", department.Description);

            return new Mapping {LeafId = leafCategory.id, PosId = department.DeptID};
        }
    }
}
