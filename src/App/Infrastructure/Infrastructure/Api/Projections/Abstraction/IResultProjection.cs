using Infrastructure.Result.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Api.Projections.Abstraction
{
    public interface IResultProjection<in TResult>
    {
        bool IsMatch(IResult<TResult> result);

        IActionResult Map(IResult<TResult> result);
    }
}
