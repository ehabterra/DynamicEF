using DynamicEF.DAL.Migrations;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using System.Reflection;

namespace DynamicEF.DAL
{
    public class DBContext : DbContext
    {
        public DBContext() : base("DBConnection")
        {

        }

        public DBContext(string sConnectionString) : base(sConnectionString)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
   
                foreach (Type type in ManageAssembly.GetAllDynamicAssemblyTypes())
                { 

                    // To ensure that the type is class and exclude types with [NotMapped] attribute
                    if (type.IsClass && type.CustomAttributes.Where(x => x.AttributeType.Name == "NotMappedAttribute").Count() == 0)
                    {

                        MethodInfo method = modelBuilder.GetType().GetMethod("Entity");

                        method = method.MakeGenericMethod(new Type[] { type });

                        method.Invoke(modelBuilder, null);

                    }

                }


            base.OnModelCreating(modelBuilder);

        }


        /// <summary>
        /// Automatic Migration at runtime
        /// </summary>
        public static void ApplyDatabaseMigrations()
        {
            //Configuration is the class created by Enable-Migrations
            DbMigrationsConfiguration dbMgConfig = new Configuration()
            {
                ContextType = typeof(DBContext)
            };
            using (var databaseContext = new DBContext())
            {
                try
                {
                    var database = databaseContext.Database;
                    var migrationConfiguration = dbMgConfig;
                    migrationConfiguration.TargetDatabase =
                        new DbConnectionInfo(database.Connection.ConnectionString,
                                             "System.Data.SqlClient");
                    var migrator = new DbMigrator(migrationConfiguration);
                    migrator.Update();
                }
                catch (AutomaticDataLossException adle)
                {
                    dbMgConfig.AutomaticMigrationDataLossAllowed = true;
                    var mg = new DbMigrator(dbMgConfig);
                    var scriptor = new MigratorScriptingDecorator(mg);
                    string script = scriptor.ScriptUpdate(null, null);
                    throw new Exception(adle.Message + " : " + script);
                }
            }
        }


        #region Invoke EF generic methods such as Set<T>() and others
        /// <summary>
        /// Invoke generic type of Set
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="type">type wanted to return</param>
        /// <returns>DBSet of type in the DB context</returns>
        public dynamic InvokeMethod_Set(Type type)
        {
            MethodInfo method_Set = this.GetType().GetMethod("Set", new Type[] { });

            method_Set = method_Set.MakeGenericMethod(new Type[] { type });

            var data_Queryable = method_Set.Invoke(this, null);
            return data_Queryable;
        }

        /// <summary>
        /// Invoke generic OfType to convert DBSet to IEnumerable
        /// </summary>
        /// <param name="type">type wanted to return</param>
        /// <returns></returns>
        public IEnumerable<object> Invoke_OfType(Type type, dynamic data_Queryable)
        {
            var method_OfType = typeof(Queryable).GetMethod("OfType");

            method_OfType = method_OfType.MakeGenericMethod(new Type[] { type });

            var dataList = (IEnumerable<object>)method_OfType.Invoke(null, new object[] { data_Queryable });
            return dataList;
        }

        /// <summary>
        /// Invoke generic Where to convert DBSet to object
        /// </summary>
        /// <param name="type">type wanted to return</param>
        /// <returns></returns>
        public object Invoke_Generic(string methodName, Type type, object data_Queryable)
        {
            var propertyMethods = typeof(Queryable)
                .GetMethods()
                .Where(m => m.Name == methodName && m.IsGenericMethodDefinition)
                .First()
                .MakeGenericMethod(new[] { type });

            var dataList = propertyMethods.Invoke(null, new object[] { data_Queryable });
            return dataList;
        }
        #endregion

    }
}
