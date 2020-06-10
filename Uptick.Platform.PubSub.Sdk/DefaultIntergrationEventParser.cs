using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uptick.Utils;

namespace Uptick.Platform.PubSub.Sdk
{
    public class DefaultIntergrationEventParser
    {
        public static IntergrationEventParsingResult Parse(byte[] message)
        {
            var serializedMessage = Encoding.UTF8.GetString(message);

            List<string> errors = new List<string>();
            JObject newObj;
            try
            {
                newObj = JObject.Parse(serializedMessage);
            }
            catch (JsonReaderException e)
            {
                return new IntergrationEventParsingResult(null, new List<string>
                {
                    $"Message is not valid JSON. {e.Message}"
                }, false);
            }

            var eventIdJToken = newObj["EventId"];
            Guid eventId;
            if (eventIdJToken == null)
            {
                errors.Add("EventId token is missed");
            }
            else
            {
                if (!Guid.TryParse(eventIdJToken.Value<string>(), out eventId))
                {
                    errors.Add("EventId is not GUID");
                }
            }

            var correlationIdJToken = newObj["CorrelationId"];
            Guid correlationId = Guid.Empty;
            if (correlationIdJToken == null)
            {
                errors.Add("CorrelationId token is missed");
            }
            else
            {
                if (!string.IsNullOrEmpty(correlationIdJToken.Value<string>()))
                {
                    if (!Guid.TryParse(correlationIdJToken.Value<string>(),
                        out correlationId))
                    {
                        errors.Add("CorrelationId is not GUID");
                    }
                    else
                    {
                        CorrelationHelper.SetCorrelationId(correlationId);
                    }
                }
            }

            var eventCreationDateJToken = newObj["EventCreationDate"];
            DateTime eventCreationDate = DateTime.MinValue;
            if (eventCreationDateJToken == null)
            {
                errors.Add("EventCreationDate token is missed");
            }
            else
            {
                try
                {
                    eventCreationDate = eventCreationDateJToken.Value<DateTime>();
                }
                catch (InvalidCastException)
                {
                    errors.Add("EventCreationDate is not DateTime");
                }
            }

            var eventTypeJToken = newObj["EventType"];
            string eventType = string.Empty;
            if (eventTypeJToken == null)
            {
                errors.Add("EventType token is missed");
            }
            else
            {
                if (string.IsNullOrEmpty(eventTypeJToken.Value<string>()))
                {
                    errors.Add("EventType can not be empty");
                }
                else
                {
                    eventType = eventTypeJToken.Value<string>();
                }
            }

            var versionJToken = newObj["Version"];
            string version = string.Empty;
            if (versionJToken == null)
            {
                errors.Add("Version token is missed");
            }
            else
            {
                if (string.IsNullOrEmpty(versionJToken.Value<string>()))
                {
                    errors.Add("Version can not be empty");
                }
                else
                {
                    version = versionJToken.Value<string>();
                }
            }

            IntergrationEventParsingResult result;
            if (errors.Count == 0)
            {
                result =
                    new IntergrationEventParsingResult(new IntegrationEvent
                    {
                        EventId = eventId,
                        EventType = eventType,
                        Version = version,
                        EventCreationDate = eventCreationDate,
                        CorrelationId = correlationId == Guid.Empty ? null :
                        (Guid?)correlationId,
                    }, new List<string>(), true);
            }
            else
            {
                result =
                    new IntergrationEventParsingResult(null, errors, false);
            }

            return result;
        }
    }
}
