using KZ.Data;
using KZ.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using PdfSharp.Pdf;
using System.Web;

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
        public IHttpActionResult NotifyAboutChangeToTasksSystem(NotifyAboutChangeToTasksSystem model)
        {
            if (model != null && ModelState.IsValid)
            {
                TasksRepository tasksRepository = new TasksRepository();
                JObject result = tasksRepository.NotifyAboutChangeToTasksSystem(model);
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

        [Route("test-redmine")]
        [HttpGet]
        [Authorize(Roles = "kz-horizontal-edit,kz-infra-edit,kz-vertical-edit,manage-tasks,manage-tasks-test")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult TestRedmine(string id = null)
        {
            try
            {
                TasksRepository tasksRepository = new TasksRepository();
                JObject result = new JObject();

                if (string.IsNullOrEmpty(id))
                {
                    // Create a simple test issue without task data
                    NotifyAboutChangeToTasksSystem testModel = new NotifyAboutChangeToTasksSystem
                    {
                        Id = "TEST-REDMINE-INTEGRATION"
                    };

                    // Create test task data
                    JObject testTaskData = new JObject();
                    JObject testAttributes = new JObject();
                    testAttributes.Add("Pavadinimas", "Test Redmine Integration");
                    testAttributes.Add("Aprasymas", "This is a test issue to verify Redmine integration works correctly");
                    testAttributes.Add("Svarba", "2");
                    testAttributes.Add("Uzduoties_tipas", "horizontal");
                    testAttributes.Add("URL", "http://localhost/test-task");
                    testTaskData.Add("attributes", testAttributes);

                    bool success = tasksRepository.TestCreateRedmineIssue(testTaskData);
                    result.Add("test_success", success);
                    result.Add("message", success ? "Test Redmine issue created successfully" : "Failed to create test Redmine issue");
                }
                else
                {
                    // Test with real task data
                    NotifyAboutChangeToTasksSystem model = new NotifyAboutChangeToTasksSystem
                    {
                        Id = id
                    };
                    JObject notifyResult = tasksRepository.NotifyAboutChangeToTasksSystem(model);
                    result.Add("task_id", id);
                    result.Add("notify_result", notifyResult);
                }

                return Ok(result);
            }
            catch (System.Exception e)
            {
                JObject errorResult = new JObject();
                errorResult.Add("error", e.Message);
                errorResult.Add("stack_trace", e.StackTrace);
                return InternalServerError();
            }
        }
    }
}