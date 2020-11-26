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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.DataProviders;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace API.Controllers
{
    /// <summary>
    /// This class is responsible for handling HTTP requests that are related
    /// to the wizard, for exampling retrieving.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WizardController : ControllerBase
    {
        private readonly ISourceManagerService sourceManagerService;
        private readonly IDataProviderService dataProviderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WizardController"/> class.
        /// </summary>
        /// <param name="sourceManagerService">The source manager service which is used to communicate with the logic layer.</param>
        public WizardController(ISourceManagerService sourceManagerService, IDataProviderService dataProviderService)
        {
            this.sourceManagerService = sourceManagerService;
            this.dataProviderService = dataProviderService;
        }

        /// <summary>
        /// This method is responsible for retrieving the wizard information.
        /// </summary>
        /// <param name="sourceURI">The source URI which is used for searching the project.</param>
        /// <returns>This method returns the filled in project.</returns>
        /// <response code="200">This endpoint returns the project with the specified source Uri.</response>
        /// <response code="400">The 400 Bad Request status code is returned when the source Uri is not specified.</response>
        /// <response code="404">The 404 Not Found status code is returned when the project could not be found with the specified source Uri.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Project), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult GetWizardInformation(Uri sourceURI)
        {
            if(sourceURI == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Source uri is null or empty.",
                    Detail = "The incoming source uri is not valid.",
                    Instance = "6D63D9FA-91D6-42D5-9ACB-461FBEB0D2ED"
                };
                return BadRequest(problem);
            }
            Project project = sourceManagerService.FetchProject(sourceURI);
            if(project == null)
            {
                return NotFound();
            }
            if(project.Name == null && project.ShortDescription == null && project.Description == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Project not found.",
                    Detail = "The incoming source uri aims at a gitlab which is either not instantiated or is a group.",
                    Instance = "E56D89C5-8760-4503-839C-F695092C79BF"
                };
                return BadRequest(problem);
            }
            return Ok(project);
        }

        public async Task<IActionResult> GetProjectsFromExternalDataSource([FromQuery] string dataSourceGuid, [FromQuery] string accessToken)
        {
            if(string.IsNullOrEmpty(dataSourceGuid))
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Invalid data source guid",
                    Detail = "Data source guid can't be empty",
                    Instance = "D84D3112-855D-480A-BCDE-7CADAC2C6C55"
                };
                return BadRequest(problem);
            }

            if(dataProviderService.IsExistingDataSourceGuid(dataSourceGuid))
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Data source guid not found",
                    Detail = "Data source could not be found with specified data source guid",
                    Instance = "4FB90F9A-8499-40F1-B7F3-3C2838BDB1D4"
                };
                return NotFound(problem);
            }

            IEnumerable<Project> projects = await dataProviderService.GetAllProjects(dataSourceGuid, accessToken);
            return Ok(projects);
        }

        public async Task<IActionResult> GetProjectByGuidFromExternalDataSource([FromQuery] string dataSourceGuid,
                                                                    [FromQuery] string accessToken,
                                                                    [FromQuery] int projectId)
        {
            if(string.IsNullOrEmpty(dataSourceGuid))
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Invalid data source guid",
                    Detail = "Data source guid can't be empty",
                    Instance = "019146D8-4162-43DD-8531-57DDD26E221C"
                };
                return BadRequest(problem);
            }

            if(dataProviderService.IsExistingDataSourceGuid(dataSourceGuid))
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Data source guid not found",
                    Detail = "Data source could not be found with specified data source guid",
                    Instance = "4E3837F4-9D35-40C4-AB7C-D325FBA225E6"
                };
                return NotFound(problem);
            }

            Project project = await dataProviderService.GetProjectByGuid(dataSourceGuid, accessToken, projectId);

            if(project == null)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Title = "Project not found",
                    Detail = "Project could not be found with specified project guid",
                    Instance = "0D96A77A-D35F-487C-B552-BF6D1C0CDD42"
                };
                return NotFound(problem);
            }

            return Ok(project);
        }
    }
}
