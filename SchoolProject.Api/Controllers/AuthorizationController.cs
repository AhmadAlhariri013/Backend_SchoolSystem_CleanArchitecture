﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolProject.Api.Base;
using SchoolProject.Core.Features.Authorization.Commands.Models;
using SchoolProject.Core.Features.Authorization.Queries.Models;
using SchoolProject.Data.AppMetaData;
using Swashbuckle.AspNetCore.Annotations;
namespace SchoolProject.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuthorizationController : AppControllerBase
    {
        [HttpPost(Router.AuthorizationRouting.Create)]
        public async Task<IActionResult> Create([FromForm] AddRoleCommand command)
        {
            var response = await Mediator.Send(command);
            return NewResult(response);
        }

        [HttpPost(Router.AuthorizationRouting.Edit)]
        public async Task<IActionResult> Edit([FromForm] EditRoleCommand command)
        {
            var response = await Mediator.Send(command);
            return NewResult(response);
        }

        [HttpDelete(Router.AuthorizationRouting.Delete)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var response = await Mediator.Send(new DeleteRoleCommand(id));
            return NewResult(response);
        }

        [HttpGet(Router.AuthorizationRouting.RolesList)]
        public async Task<IActionResult> GetRoleList()
        {
            var response = await Mediator.Send(new GetRolesListQuery());
            return NewResult(response);
        }

        [SwaggerOperation(Summary = "idالصلاحية عن طريق ال", OperationId = "RoleById")]
        [HttpGet(Router.AuthorizationRouting.RoleById)]
        public async Task<IActionResult> GetRoleById([FromRoute] int id)
        {
            var response = await Mediator.Send(new GetRoleByIdQuery(id));
            return NewResult(response);
        }

        [SwaggerOperation(Summary = " صلاحيات المستخدمين", OperationId = "ManageUserRoles")]
        [HttpGet(Router.AuthorizationRouting.GetUserRoles)]
        public async Task<IActionResult> GetUserRoles([FromRoute] int userId)
        {
            var response = await Mediator.Send(new GetUserRolesQuery() { UserId = userId });
            return NewResult(response);
        }

        [SwaggerOperation(Summary = " تعديل صلاحيات المستخدمين", OperationId = "UpdateUserRoles")]
        [HttpPut(Router.AuthorizationRouting.UpdateUserRoles)]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesCommand command)
        {
            var response = await Mediator.Send(command);
            return NewResult(response);
        }

        [SwaggerOperation(Summary = " صلاحيات الاستخدام للمستخدمين", OperationId = "ManageUserClaims")]
        [HttpGet(Router.AuthorizationRouting.GetUserClaims)]
        public async Task<IActionResult> GetUserClaims([FromRoute] int userId)
        {
            var response = await Mediator.Send(new GetUserClaimsQuery() { UserId = userId });
            return NewResult(response);
        }


        [SwaggerOperation(Summary = " تعديل صلاحيات  الاستخدام للمستخدمين", OperationId = "UpdateUserClaims")]
        [HttpPut(Router.AuthorizationRouting.UpdateUserClaims)]
        public async Task<IActionResult> UpdateUserClaims([FromBody] UpdateUserClaimsCommand command)
        {
            var response = await Mediator.Send(command);
            return NewResult(response);
        }
    }
}