using System.Linq.Expressions;

namespace Application.Contracts;

public interface IRecurringJobFactory
{
    void CreateJob<T>(string jobId, Expression<Action<T>> method, string cronExpression);
}