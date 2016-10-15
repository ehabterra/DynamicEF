using DynamicEF.DAL;
using System;
using System.Activities;

namespace DynamicEF.WorkflowHost.Activities
{
    public class AddData<T> : CodeActivity where T : class
    {
        public InArgument<DBContext> DbContext { get; set; }
        public InArgument<T> data { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            if (data.Get(context) != null)
            {
                var db = DbContext.Get(context);

                db.Set<T>().Add(data.Get(context));
                db.Entry(data.Get(context)).State = System.Data.Entity.EntityState.Added;
            }
            else
            {
                throw new Exception("No Data Specified.");
            }
        }
    }
}