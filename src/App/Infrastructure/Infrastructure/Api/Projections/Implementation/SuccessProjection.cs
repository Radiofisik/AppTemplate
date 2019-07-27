using Infrastructure.Api.Projections.Abstraction;
using Infrastructure.Result.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Api.Projections.Implementation
{
    public class SuccessProjection<TResult>: IResultProjection<TResult>
    {
        public bool IsMatch(IResult<TResult> result) => result is ISuccess<TResult>;

        public IActionResult Map(IResult<TResult> result)
        {
            var success = result as ISuccess<TResult>;
            return new ObjectResult(success.Value);
        }
    }
}
