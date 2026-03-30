using System.Linq.Expressions;
using Application.Contracts;
using Hangfire;

namespace App.Factories;

/// <summary>
/// 
/// </summary>
/// <param name="recurringJobManager"></param>
public class RecurringJobFactory(IRecurringJobManager recurringJobManager) : IRecurringJobFactory
{
    /// <summary>
    /// 
    /// </summary>
    public void CreateJob<T>(string jobId, Expression<Action<T>> method, string cronExpression)
    {
        recurringJobManager.AddOrUpdate(jobId, method, cronExpression);
    }
}