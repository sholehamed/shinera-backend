using Application.Sharedkernel.Models;
using Domain.Sharedkernel.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Web.Sharedkernel.Util
{
    public static class ResultExtensions
    {
        public static IResult ToHttpResult<T>(
            this Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse<T>.Ok(result.Value));
            }

            return result.Error.Type switch
            {
                ErrorType.Validation =>
                    Results.Json(
                        ApiResponse<T>.Fail(
                            result.Error.Code,
                            result.Error.Message),
                        statusCode: StatusCodes.Status400BadRequest),

                ErrorType.NotFound =>
                    Results.Json(
                        ApiResponse<T>.Fail(
                            result.Error.Code,
                            result.Error.Message),
                        statusCode: StatusCodes.Status404NotFound),

                ErrorType.Conflict =>
                    Results.Json(
                        ApiResponse<T>.Fail(
                            result.Error.Code,
                            result.Error.Message),
                        statusCode: StatusCodes.Status409Conflict),

                ErrorType.Unauthorized =>
                    Results.Json(
                        ApiResponse<T>.Fail(
                            result.Error.Code,
                            result.Error.Message),
                        statusCode: StatusCodes.Status401Unauthorized),

                ErrorType.Forbidden =>
                    Results.Json(
                        ApiResponse<T>.Fail(
                            result.Error.Code,
                            result.Error.Message),
                        statusCode: StatusCodes.Status403Forbidden),

                _ =>
                    Results.Json(
                        ApiResponse<T>.Fail(
                            result.Error.Code,
                            result.Error.Message),
                        statusCode: StatusCodes.Status500InternalServerError)
            };
        }
    }
}
