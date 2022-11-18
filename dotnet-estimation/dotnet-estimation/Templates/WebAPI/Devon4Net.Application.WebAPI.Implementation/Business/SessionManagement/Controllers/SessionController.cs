using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Devon4Net.Application.WebAPI.Implementation.Business.SessionManagement.Exceptions;
using Devon4Net.Application.WebAPI.Implementation.Business.SessionManagement.Converters;
using Devon4Net.Application.WebAPI.Implementation.Business.SessionManagement.Service;
using Devon4Net.Application.WebAPI.Implementation.Business.SessionManagement.Dtos;
using Devon4Net.Application.WebAPI.Implementation.Domain.Entities;
using Devon4Net.Infrastructure.Logger.Logging;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Devon4Net.Infrastructure.JWT.Common.Const;
using System.Net;
using Task = System.Threading.Tasks.Task;
using System.Net.WebSockets;
using LiteDB;

namespace Devon4Net.Application.WebAPI.Implementation.Business.SessionManagement.Controllers
{
    /// <summary>
    /// Session controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    
    public class SessionController: ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IWebSocketHandler _webSocketHandler;
        
        public SessionController(ISessionService SessionService, IWebSocketHandler webSocketHandler)
        {
            _sessionService = SessionService;
            _webSocketHandler = webSocketHandler;
        }
        
        

        /// <summary>
        /// Creates a session
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("/estimation/v1/session/newSession")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateSession(SessionDto sessionDto)
        {
            Devon4NetLogger.Debug($"Create session that will expire at {sessionDto.ExpiresAt}");
            var result = await _sessionService.CreateSession(sessionDto);
            return StatusCode(StatusCodes.Status200OK, LiteDB.JsonSerializer.Serialize(result));
        }
        [HttpPut]
        [AllowAnonymous]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("/estimation/v1/session/{id:long}/invalidate")]
        public async Task<IActionResult> InvalidateSession(long id)
        {
            Devon4NetLogger.Debug($"Put-Request to invalidate session with id: {id}");

            try
            {
                return Ok(await _sessionService.InvalidateSession(id));
            }
            catch (Exception exception)
            {
                return exception switch
                {
                    NotFoundException _ => NotFound(),
                    InvalidSessionException _ => BadRequest(),
                    _ => StatusCode(500)
                };
            }
        }

        /// <summary>
        /// Remove a Session user
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("/estimation/v1/session/{sessionId:long}/leaveSession/{userId}")]
        public async Task<IActionResult> RemoveUserFromSession(long sessionId, String userId)
        {
            Devon4NetLogger.Debug($"Put-Request for removing user with id: {userId} from session status with id: {sessionId}");

            try
            {
                var leaveResult = await _sessionService.RemoveUserFromSession(sessionId, userId);

                return new ObjectResult(JsonConvert.SerializeObject(leaveResult));
            }
            catch (Exception exception)
            {
                return exception switch
                {
                    NotFoundException _ => StatusCode(500),
                };
            }
        }

        /// <summary>
        /// Add a Session user
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(AuthenticationSchemes = AuthConst.AuthenticationScheme, Roles = $"Author,Moderator,Voter")]
        [Route("/estimation/v1/session/{sessionId:long}/entry")]
        public async Task<ActionResult<UserDto>> AddUserToSession(long sessionId, UserDto userDto)
        {
            //Get claims
            var token = Request.Headers["Authorization"].ToString().Replace($"{AuthConst.AuthenticationScheme} ", string.Empty);

            if (string.IsNullOrEmpty(token)) return Unauthorized();

            Devon4NetLogger.Debug("Executing AddUserToSession from controller SessionController");
            var result = await _sessionService.AddUserToSession(sessionId, userDto.Id,
                userDto.Role).ConfigureAwait(false);
            return StatusCode(StatusCodes.Status201Created, result);
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("/estimation/v1/session/{sessionId:long}/task")]
        public async Task<ActionResult> AddTask(long sessionId, [FromBody]TaskDto task)
        {
            var finished = await _sessionService.AddTaskToSession(sessionId, task);

            if (finished)
            {
                Message<TaskDto> Message = new Message<TaskDto>
                {
                    Type = MessageType.TaskCreated,
                    Payload = task
                };
                await _webSocketHandler.Send(Message, sessionId);
                return Ok();
            }
            return BadRequest();
        }
        /// <summary>
        /// Add a Session Esstimation 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(EstimationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("/estimation/v1/session/{sessionId:long}/estimation")]

        public async Task<ActionResult<EstimationDto>> AddNewEstimation(long sessionId, EstimationDto estimationDto)
        {
            Devon4NetLogger.Debug("Executing AddNewEstimation from controller SessionController");
            var result = await _sessionService.AddNewEstimation(sessionId, estimationDto.VoteBy, estimationDto.Complexity).ConfigureAwait(false);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
