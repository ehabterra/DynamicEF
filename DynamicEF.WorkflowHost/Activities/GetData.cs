using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using AutoMapper;
using DynamicEF.DAL;

namespace DynamicEF.WorkflowHost.Activities
{
    public class GetData<T, TResult> : CodeActivity where T : class
    {
        public InArgument<DBContext> DbContext { get; set; }
        public InArgument<Expression<Func<T, bool>>> filter { get; set; }
        public OutArgument<TResult> data { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var db = DbContext.Get(context);

            List<T> result;

            if (filter.Get(context) == null)
                result = db.Set<T>().ToList();
            else
                result = db.Set<T>().Where(filter.Get(context)).ToList();

            Mapper.Initialize(x => x.CreateMap<T, TResult>());

            // auto mapper
            TResult dto = Mapper.Map<TResult>(result);

            data.Set(context, dto);

        }
    }
}