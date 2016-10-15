using DynamicEF.DAL;
using System;
using System.Activities;

namespace DynamicEF.WorkflowHost.Activities
{
    public class SaveData : CodeActivity
    {
        public InArgument<DBContext> DbContext { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var db = DbContext.Get(context);

            db.SaveChanges();
        }
    }
}