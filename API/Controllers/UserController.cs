/*
* Digital Excellence Copyright (C) 2020 Brend Smits
* 
* This program is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation version 3 of the License.
* 
* This program is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty 
* of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
* See the GNU Lesser General Public License for more details.
* 
* You can find a copy of the GNU Lesser General Public License 
* along with this program, in the LICENSE.md file in the root project directory.
* If not, see https://www.gnu.org/licenses/lgpl-3.0.txt
*/

using API.Extensions;
using API.Resources;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Defaults;
using RestSharp;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{

    /// <summary>
    ///     This controller handles the user settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly IMapper mapper;
        private readonly IUserService userService;
        private readonly IProjectService projectService;

        /// <summary>
        ///     Initialize a new instance of UserController
        /// </summary>
        /// <param name="userService"></param>
        /// <param name="projectService"></param>
        /// <param name="mapper"></param>
        public UserController(IUserService userService, IProjectService projectService, IMapper mapper)
        {
            this.userService = userService;
            this.projectService = projectService;
            this.mapper = mapper;
        }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            string identityId = HttpContext.User.GetIdentityId(HttpContext);
            User user = await userService.GetUserByIdentityIdAsync(identityId);
            if(user == null)
            {
                ProblemDetails problem = new ProblemDetails
                 {
                     Title = "Failed getting the user account.",
                     Detail = "The user could not be found in the database.",
                     Instance = "A4C4EEFA-1D3E-4E64-AF00-76C44D805D98"
                };
                return NotFound(problem);
            }
            return Ok(mapper.Map<User, UserResourceResult>(user));
        }

        /// <summary>
        ///     Get a user account.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        [Authorize(Policy = nameof(Defaults.Scopes.UserRead))]
        public async Task<IActionResult> GetUser(int userId)
        {
            if(userId < 0)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Failed getting the user account.",
                    Detail = "The user id is less then zero and therefore cannot exist in the database.",
                    Instance = "EAF7FEA1-47E9-4CF8-8415-4D3BC843FB71",
                };
                return BadRequest(problem);
            }

            User user = await userService.FindAsync(userId);
            if(user == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Failed getting the user account.",
                    Detail = "The user could not be found in the database.",
                    Instance = "140B718F-9ECD-4F68-B441-F85C1DC7DC32"
                };
                return NotFound(problem);
            }

            return Ok(mapper.Map<User, UserResourceResult>(user));
        }


        /// <summary>
        ///     Create a user account.
        /// </summary>
        /// <param name="accountResource"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = nameof(Defaults.Scopes.UserWrite))]
        public IActionResult CreateAccount([FromBody] UserResource accountResource)
        {
            User user = mapper.Map<UserResource, User>(accountResource);
            try
            {
                userService.Add(user);
                userService.Save();
                return Created(nameof(CreateAccount), mapper.Map<User, UserResourceResult>(user));
            } catch
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Failed to create user account.",
                    Detail = "Failed saving the user account to the database.",
                    Instance = "D8C786C1-9E6D-4D36-83F4-A55D394B5017"
                };
                return BadRequest(problem);
            }
        }

        /// <summary>
        ///     Update the User account.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userResource"></param>
        /// <returns></returns>
        [HttpPut("{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateAccount(int userId, [FromBody] UserResource userResource)
        {
            User currentUser = await HttpContext.GetContextUser(userService).ConfigureAwait(false);
            bool isAllowed = userService.UserHasScope(currentUser.IdentityId, nameof(Defaults.Scopes.UserWrite));

            if(currentUser.Id != userId && !isAllowed)
            {
                ProblemDetails problem = new ProblemDetails
                 {
                     Title = "Failed to edit the user.",
                     Detail = "The user is not allowed to edit this user.",
                     Instance = "E28BEBC0-AE7C-49F5-BDDC-3C13972B75D0"
                 };
                return Unauthorized(problem);
            }


            User user = await userService.FindAsync(userId);
            if(user == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Failed getting the user account.",
                    Detail = "The database does not contain a user with that user id.",
                    Instance = "EF4DA55A-C31A-4BC4-AE30-098DEB0D3457"
                };
                return NotFound(problem);
            }

            mapper.Map(userResource, user);

            userService.Update(user);
            userService.Save();

            return Ok(mapper.Map<User, UserResourceResult>(user));
        }

        /// <summary>
        ///     Delete the user account.
        /// </summary>
        /// <returns></returns>
        [ObsoleteAttribute("This endpoint will soon be deprecated. Use [Post] {userId}/allData.")]
        [HttpDelete("{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(int userId)
        {

            User user = await HttpContext.GetContextUser(userService).ConfigureAwait(false);
            bool isAllowed = userService.UserHasScope(user.IdentityId, nameof(Defaults.Scopes.UserWrite));

            if(user.Id != userId && !isAllowed)
            {
                ProblemDetails problem = new ProblemDetails
                 {
                     Title = "Failed to delete the user.",
                     Detail = "The user is not allowed to delete this user.",
                     Instance = "26DA6D58-DB7B-467D-90AA-69EFBF55A83C"
                 };
                return Unauthorized(problem);
            }

            if(await userService.FindAsync(userId) == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Failed getting the user account.",
                    Detail = "The database does not contain a user with this user id.",
                    Instance = "C4C62149-FF9A-4E4C-8C9F-6BBF518BA085"
                };
                return NotFound(problem);
            }

            await userService.RemoveAsync(userId);
            userService.Save();
            return Ok();
        }

        /// <summary>
        ///     Delete all user data
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{userId}/allData")]
        [Authorize(Policy = nameof(Defaults.Scopes.UserWrite))]
        public async Task<IActionResult> DeleteAllUserData(int userId)
        {
            if(await userService.FindAsync(userId) == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Failed getting the user account.",
                    Detail = "The database does not contain a user with this student id.",
                    Instance = "TODO-CHANGE-TO-GENERATED-INSTANCE-CODE"
                };
                return NotFound(problem);
            }
            List<Project> projects = await projectService.GetAllWithUsersAsync();
            projects.ForEach(delegate(Project project) {
                if(project.UserId.Equals(userId))
                {
                    project.UserId = -1;
                    projectService.Update(project);
                }
            });
            await userService.RemoveAsync(userId);
            userService.Save();
            projectService.Save();
            return Ok();
        }






    }
}
