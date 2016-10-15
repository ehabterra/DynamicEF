using DynamicEF.DAL;
using System;
using System.Activities;
using System.Data.Entity;

namespace DynamicEF.WorkflowHost.Activities
{
    public class UpdateDatabase : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            // Update db
            try
            {
                DBContext.ApplyDatabaseMigrations();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}