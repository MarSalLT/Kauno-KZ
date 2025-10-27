using KZ.Data;
using KZ.Models;
using Newtonsoft.Json.Linq;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace KZ.Controllers.Api
{
    [RoutePrefix("web-services/tasks")]
    public class TaskController : ApiController
    {
        [Route("get-tasks-list-g")]
        [HttpGet]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetTasksListGeneral()
        {
            TasksRepository tasksRepository = new TasksRepository();
            List<Dictionary<string, object>> result = tasksRepository.GetTasksListForUser();
            return Ok(result);
        }
        [Route("get-task-data-g")]
        [HttpGet]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetTaskDataGeneral(string id, bool returnAttachments = true, bool modAttributes = false)
        {
            if (!string.IsNullOrEmpty(id))
            {
                TasksRepository tasksRepository = new TasksRepository();
                JObject result = tasksRepository.GetTaskData(id, returnAttachments, false, null, false, modAttributes);
                return Ok(result);
            }
            return BadRequest();
        }
        [Route("attachments/{taskId}/{attachmentId}")]
        [HttpGet]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetAttachment(int taskId, int attachmentId)
        {
            // TODO: idealiu atveju reiktų pagal taskId gauti teritorijos kodą... Ir toliau tęsti tik, jei dabartiniam vartotojui ta teritorija priklauso...
            TasksRepository tasksRepository = new TasksRepository();
            var r = tasksRepository.GetAttachment(taskId, attachmentId);
            return ResponseMessage(r);
        }
        [Route("notify-about-change-to-tasks-system")]
        [HttpPost]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> NotifyAboutChangeToTasksSystem(NotifyAboutChangeToTasksSystem model)
        {
            if (model != null && ModelState.IsValid)
            {
                TasksRepository tasksRepository = new TasksRepository();
                JObject result = await tasksRepository.NotifyAboutChangeToTasksSystem(model);
                return Ok(result);
            }
            return BadRequest();
        }
        [Route("log-task-view-action")]
        [HttpPost]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult LogTaskViewAction(TaskModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                string userId = HttpContext.Current.Session["id"].ToString();
                if (!string.IsNullOrEmpty(userId))
                {
                    TasksRepository tasksRepository = new TasksRepository();
                    JObject result = tasksRepository.LogTaskViewAction(model.Id, userId);
                    return Ok(result);
                }
            }
            return BadRequest();
        }
        [Route("get-report")]
        [HttpGet]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")] // Testavimui atkomentuojame...
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetPDF(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                TasksRepository tasksRepository = new TasksRepository();
                PdfDocument document = tasksRepository.GetReport(id);
                if (document != null)
                {
                    MemoryStream stream = new MemoryStream();
                    document.Save(stream, false);
                    var r = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(stream)
                    };
                    r.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                    r.Content.Headers.ContentLength = stream.Length;
                    return ResponseMessage(r);
                }
            }
            return BadRequest();
        }
        [Route("{globalId}")]
        [HttpPut]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult UpdateTask(string globalId, [FromBody] JObject updateData)
        {
            if (string.IsNullOrEmpty(globalId))
            {
                return BadRequest("globalId is required");
            }

            if (updateData != null)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTask called for globalId: {globalId}");
                System.Diagnostics.Debug.WriteLine($"UpdateData: {updateData}");

                // Extract the actual task data from the nested structure
                JObject taskData = updateData["updateData"] as JObject;

                if (taskData == null)
                {
                    return BadRequest("updateData property is missing or invalid");
                }

                System.Diagnostics.Debug.WriteLine($"TaskData: {taskData}");

                // Build the update payload for apply-edits endpoint
                JArray editsArray = new JArray();
                JObject layerEdit = new JObject();
                layerEdit["id"] = 0; // Layer ID for tasks (adjust if needed)

                JArray updatesArray = new JArray();
                JObject featureUpdate = new JObject();
                JObject attributes = new JObject();
                attributes["GlobalID"] = globalId;

                // Map fields from Camunda worker to ArcGIS attributes
                if (taskData.ContainsKey("description") && taskData["description"] != null)
                {
                    string description = taskData["description"].ToString();
                    if (!string.IsNullOrEmpty(description))
                    {
                        attributes["Aprasymas"] = description;
                    }
                }

                if (taskData.ContainsKey("subject") && taskData["subject"] != null)
                {
                    string subject = taskData["subject"].ToString();
                    if (!string.IsNullOrEmpty(subject))
                    {
                        attributes["Pavadinimas"] = subject;
                    }
                }

                if (taskData.ContainsKey("statusId") && taskData["statusId"] != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Statusas: {taskData["statusId"]}");
                    int status = Convert.ToInt32(taskData["statusId"]);
                    int arcgisStatus;

                    switch (status)
                    {
                        case 1:
                            arcgisStatus = 6;
                            break;
                        case 2:
                            arcgisStatus = 1;
                            break;
                        case 3:
                            arcgisStatus = 4;
                            break;
                        case 5:
                            arcgisStatus = 5;
                            break;
                        case 6:
                            arcgisStatus = 5;
                            break;
                        // Add more cases as needed
                        default:
                            arcgisStatus = 0;
                            break;
                    }

                    attributes["Statusas"] = arcgisStatus;
                    System.Diagnostics.Debug.WriteLine($"Mapping Statusas: {status} -> {arcgisStatus}");
                }

                featureUpdate["attributes"] = attributes;
                updatesArray.Add(featureUpdate);
                layerEdit["updates"] = updatesArray;
                editsArray.Add(layerEdit);

                // Call FeatureRepository.ApplyEdits directly (same as /web-services/feature/apply-edits)
                FeatureRepository featureRepository = new FeatureRepository();
                JObject result = featureRepository.ApplyEdits("tasks", editsArray);

                System.Diagnostics.Debug.WriteLine($"ApplyEdits result: {result}");
                return Ok(result);
            }

            return BadRequest("Update data is required");
        }
        [Route("approve-or-reject-task")]
        [HttpPost]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult ApproveOrRejectTask(TaskApprovalOrRejectionModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                TasksRepository tasksRepository = new TasksRepository();
                JObject result = tasksRepository.ApproveOrRejectTask(model);
                return Ok(result);
            }
            return BadRequest();
        }
    }
}