using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;
using System.Reflection;

namespace DynamicEF.DAL.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestCreateDll()
        {
            try
            {
                string sourceLocation = "";
                string targetLocation = "";
                string location = "";

                ManageAssembly.GetWorkPaths(out sourceLocation, out targetLocation, out location);

                ManageAssembly.CreateAssemblies();

                Assert.IsTrue(File.Exists(Path.Combine(targetLocation, "table1.dll")));

            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestCreateOrUpdateDB()
        {
            try
            {
                DBContext.ApplyDatabaseMigrations();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestGetType()
        {
            try
            {
                var types = ManageAssembly.GetAllDynamicAssemblyTypes();

                Assert.AreEqual(1, types.Where(x => x.Name == "table1").Count());
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestInsertDataInDB()
        {
            try
            {
                using (var context = new DBContext())
                {
                    Type table1 = ManageAssembly.GetType("table1");

                    var data = context.InvokeMethod_Set(table1);

                    // New instance of table1
                    dynamic instance = Activator.CreateInstance(table1);

                    // add title property
                    instance.Title = "Test1";

                    // add new
                    data.Add(instance);

                    // save to DB
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestGetDataFromDB()
        {
            try
            {
                using (var context = new DBContext())
                {
                    Type table1 = ManageAssembly.GetType("table1");

                    #region Invoke "Set" from context and then Invoke "FirstOrDefault" 
                    // Get DBSet
                    dynamic data_Queryable = context.InvokeMethod_Set(table1);

                    dynamic data = context.Invoke_Generic("FirstOrDefault", table1, data_Queryable);
                    #endregion

                    Assert.AreEqual("Test1", data.Title);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
