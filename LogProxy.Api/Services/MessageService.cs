using LogProxy.Api.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LogProxy.Api
{
    public class MessageService : IMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _messageUri;

        public MessageService(HttpClient httpClient, IConfiguration configuration)
        {
            _messageUri = configuration.GetValue<string>("MessagesApiUrl");
            var authenticationKey = configuration.GetValue<string>("MessagesApiKey");
            this._httpClient = GetHttpClient(httpClient, authenticationKey);            
        }

        public async Task<List<MessageDto>> GetMessagesAsync()
        {
            var response = await _httpClient.GetAsync(new Uri(_messageUri));
            var messageData = JsonConvert.DeserializeObject<MessageData>(
                await response.Content.ReadAsStringAsync());

            return MapToMessageDtos(messageData);
        }

        public async Task<List<MessageDto>> PostMessagesAsync(List<MessageDto> messages)
        {
            if (!messages?.Any() == true) return new List<MessageDto>();

            FillMissingFields(messages);

            var response = await _httpClient.PostAsync(
                new Uri(_messageUri),
                GetHttpContent(messages));

            var messageData = JsonConvert.DeserializeObject<MessageData>(
                await response.Content.ReadAsStringAsync());

            return MapToMessageDtos(messageData);
        }

        private void FillMissingFields(List<MessageDto> messages)
        {
            messages.ForEach(m => 
            {
                m.Id = Guid.NewGuid().ToString();
                m.ReceivedAt = DateTime.Now;
            });
        }
        private HttpContent GetHttpContent(List<MessageDto> messages)
        {
            return new StringContent(
                JsonConvert.SerializeObject(
                    MapToMessageData(messages),
                    Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
                    ),
                Encoding.UTF8,
                "application/json"
                );
        }

        private MessageData MapToMessageData(List<MessageDto> messageDtos)
        {
            var messageData = new MessageData();

            if (!messageDtos?.Any() == true) return messageData;

            messageDtos.ForEach(m => messageData.Records.Add(MapToFieldData(m)));
            return messageData;
        }

        private RecordData MapToFieldData(MessageDto messageDto)
        {
            return new RecordData
            {
                FieldData = new FieldData
                {
                    Id = messageDto.Id,
                    Summary = messageDto.Title,
                    Message = messageDto.Text,
                    ReceivedAt = messageDto.ReceivedAt
                }
            };
        }

        private List<MessageDto> MapToMessageDtos(MessageData messageData)
        {
            List<MessageDto> messages = new List<MessageDto>();
            if (!messageData.Records.Any()) return messages;

            messageData.Records.ForEach(r => messages.Add(MapToMessageDto(r)));
            return messages;
        }

        private static MessageDto MapToMessageDto(RecordData recordData)
        {
            return new MessageDto
            {
                Id = recordData?.FieldData?.Id,
                Title = recordData?.FieldData?.Summary,
                Text = recordData?.FieldData?.Message,
                ReceivedAt = recordData?.FieldData?.ReceivedAt
            };
        }

        private HttpClient GetHttpClient(HttpClient httpClient, string authenticationKey)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationKey);
            return httpClient;
        }
    }
}