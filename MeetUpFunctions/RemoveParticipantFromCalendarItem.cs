using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web.Http;
using MeetUpPlanner.Shared;
using System.Collections.Generic;

namespace MeetUpPlanner.Functions
{
    public class RemoveParticipantFromCalendarItem
    {
        private readonly ILogger _logger;
        private ServerSettingsRepository _serverSettingsRepository;
        private CosmosDBRepository<Participant> _cosmosRepository;
        private CosmosDBRepository<CalendarItem> _calendarRepository;
        private NotificationSubscriptionRepository _subscriptionRepository;

        public RemoveParticipantFromCalendarItem(ILogger<RemoveParticipantFromCalendarItem> logger,
                                            ServerSettingsRepository serverSettingsRepository,
                                            NotificationSubscriptionRepository subscriptionRepository,
                                            CosmosDBRepository<CalendarItem> calendarRepository,
                                            CosmosDBRepository<Participant> cosmosRepository
                                            )
        {
            _logger = logger;
            _serverSettingsRepository = serverSettingsRepository;
            _cosmosRepository = cosmosRepository;
            _calendarRepository = calendarRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        [FunctionName("RemoveParticipantFromCalendarItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation($"C# HTTP trigger function RemoveParticipantFromCalendarItem processed a request.");
            string tenant = req.Headers[Constants.HEADER_TENANT];
            if (String.IsNullOrWhiteSpace(tenant))
            {
                tenant = null;
            }
            ServerSettings serverSettings = await _serverSettingsRepository.GetServerSettings(tenant);

            string keyWord = req.Headers[Constants.HEADER_KEYWORD];
            if (String.IsNullOrEmpty(keyWord) || !(serverSettings.IsUser(keyWord) || _serverSettingsRepository.IsInvitedGuest(keyWord)))
            {
                return new BadRequestErrorMessageResult("Keyword is missing or wrong.");
            }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Participant participant = JsonConvert.DeserializeObject<Participant>(requestBody);
            if (String.IsNullOrEmpty(participant.Id))
            {
                return new OkObjectResult(new BackendResult(false, "Die Id des Teilnehmers fehlt."));
            }
            // Get most current version from database to be sure that the participant still exists and to get the actual waiting status
            participant = await _cosmosRepository.GetItem(participant.Id);
            if (null == participant)
            {
                // nothing to do, participant is already removed
                return new OkObjectResult(new BackendResult(true));
            }
            // Check if there is someone on waiting list who can be promoted now. But only if removed participant is not from waiting list
            if (!participant.IsWaiting)
            {
                IEnumerable<Participant> participants = await _cosmosRepository.GetItems(p => p.CalendarItemId.Equals(participant.CalendarItemId));
                int counter = 0;
                int coGuideCounter = 0;
                foreach (Participant p in participants)
                {
                    if (!p.IsWaiting) counter++;
                    if (!p.Id.Equals(participant.Id) && p.IsWaiting)
                    {
                        CalendarItem calendarItem = await _calendarRepository.GetItem(p.CalendarItemId);
                        int maxRegistrationCount = calendarItem.MaxRegistrationsCount;
                        if (p.IsCoGuide)
                        {
                            maxRegistrationCount += (calendarItem.MaxCoGuidesCount - coGuideCounter);
                        }
                        if (counter < maxRegistrationCount)
                        {
                            p.IsWaiting = false;
                            await _cosmosRepository.UpsertItem(p);
                            await _subscriptionRepository.NotifyParticipant(calendarItem, p, "Das Warten hat sich gelohnt - Du bist jetzt angemeldet.");
                        }
                        break; // only the first one from waiting list can be promoted
                    }
                    if (p.IsCoGuide) coGuideCounter++;
                }
            }
            await _cosmosRepository.DeleteItemAsync(participant.Id);

            return new OkObjectResult(new BackendResult(true));
        }
    }
}

