using Granify.Api.DataAccess;
using Granify.Models;
using Granify.Providers;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace GranifyTest
{
    public class ItemRepoTest
    {
        protected AirTableResponseItem _deletedItem = new AirTableResponseItem
        {
            Id = "item1",
            Item = new Item
            {
                Id = "atItem",
                IsDeleted = true,
                LastUpdated = DateTime.Now,
                Name = "atDeletedItem",
                PhoneNumber = "780-246-8060"
            }
        };

        protected AirTableResponseItem _activeItem = new AirTableResponseItem
        {
            Id = "item2",
            Item = new Item
            {
                Id = "atItem2",
                IsDeleted = false,
                LastUpdated = DateTime.Now,
                Name = "atActiveItem",
                PhoneNumber = "780-246-8060"
            }
        };

        protected AirTableResponseItem _oldItem = new AirTableResponseItem
        {
            Id = "item2",
            Item = new Item
            {
                Id = "atItem2",
                IsDeleted = false,
                LastUpdated = DateTime.Now.AddHours(-4),
                Name = "atActiveItem",
                PhoneNumber = "780-246-8060"
            }
        };

        #region GetRowsAsync
        [Fact]
        public async Task GetRowsAsync_DefaultFilter()
        {
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new AirTableResponse { Records = new List<AirTableResponseItem> {_activeItem,_deletedItem } }));

            var itemRepo = new ItemRepo(airTableClient.Object);
            var response = await itemRepo.GetRowsAsync();
            Assert.NotNull(response);
            Assert.Single(response);
            Assert.Equal(_activeItem.Id, response.First().Id);
        }


        [Fact]
        public async Task GetRowsAsync_NoFilter()
        {
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new AirTableResponse { Records = new List<AirTableResponseItem> { _activeItem, _deletedItem } }));

            var itemRepo = new ItemRepo(airTableClient.Object);
            var response = await itemRepo.GetRowsAsync(true);
            Assert.NotNull(response);
            Assert.Equal(2, response.Count());
        }

        #endregion

        #region GetRowById
        [Fact]
        public async Task GetRowById_NotFoundEx()
        {
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new AirTableResponse { Records = new List<AirTableResponseItem> { } }));

            var itemRepo = new ItemRepo(airTableClient.Object);
            var exception = await Record.ExceptionAsync(async () => await itemRepo.GetRowById(""));
            Assert.NotNull(exception);
            Assert.IsType<KeyNotFoundException>(exception);

        }

        [Fact]
        public async Task GetRowById_FilterDeleted()
        {
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new AirTableResponse { Records = new List<AirTableResponseItem> { _deletedItem } }));

            var itemRepo = new ItemRepo(airTableClient.Object);
            var exception = await Record.ExceptionAsync(async () => await itemRepo.GetRowById(""));
            Assert.NotNull(exception);
            Assert.IsType<KeyNotFoundException>(exception);
        }


        [Fact]
        public async Task GetRowById_Return()
        {
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.GetStringAsync(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new AirTableResponse { Records = new List<AirTableResponseItem> { _activeItem } }));

            var itemRepo = new ItemRepo(airTableClient.Object);
            var response = await itemRepo.GetRowById("");
            Assert.NotNull(response);
            Assert.Equal(_activeItem.Id, response.Id);
        }
        #endregion

        #region GetStatistics
        [Fact]
        public async Task GetStatisticsTimeFilter()
        {
            var itemRepo = new Mock<ItemRepo>(null);
            itemRepo.CallBase = true;

            itemRepo.Setup(i => i.GetRowsAsync(true)).ReturnsAsync(new List<AirTableResponseItem> { _activeItem, _deletedItem, _oldItem} );
            var response = await itemRepo.Object.GetItemStatistics();
            Assert.NotNull(response);
            Assert.Equal(1, response.ActiveCount);
            Assert.Equal(1, response.DeletedCount);
            
        }

        #endregion

        #region DeleteItemAsync
        [Fact]
        public async Task DeleteItemAsync_Failure()
        {
            var airtableClient = new Mock<AirTableClientProvider>("");
            airtableClient.Setup(a => a.PatchAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            var itemRepo = new Mock<ItemRepo>(airtableClient.Object);
            itemRepo.CallBase = true;
            itemRepo.Setup(i => i.GetRowById(It.IsAny<string>())).ReturnsAsync(_activeItem);
            var exception = await Record.ExceptionAsync(async () => await itemRepo.Object.DeleteItemAsync(""));
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task DeleteItemAsync_Success()
        {
            var airtableClient = new Mock<AirTableClientProvider>("");
            airtableClient.Setup(a => a.PatchAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var itemRepo = new Mock<ItemRepo>(airtableClient.Object);
            itemRepo.CallBase = true;
            itemRepo.Setup(i => i.GetRowById(It.IsAny<string>())).ReturnsAsync(_activeItem);
            var exception = await Record.ExceptionAsync(async () => await itemRepo.Object.DeleteItemAsync(""));
            Assert.Null(exception);
        }
        #endregion

        #region PostItemAsync
        [Fact]
        public async Task PostItemAsync_InvalidValues()
        {
            var emptyNameItem = new Item { Name = "", PhoneNumber = "780-233-4555" };
            var itemRepo = new ItemRepo(null);

            var exception = await Record.ExceptionAsync(async () => await itemRepo.PostItemAsync(emptyNameItem));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);

            var emptyPhoneNumber = new Item { Name = "Jordan", PhoneNumber = "" };
            exception = await Record.ExceptionAsync(async () => await itemRepo.PostItemAsync(emptyPhoneNumber));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public async Task PostItemAsync_InvalidPhoneNumber()
        {
            var invalidPhoneNumber = new Item { Name = "Jordan", PhoneNumber = "780-3-4555" };
            var itemRepo = new ItemRepo(null);

            var exception = await Record.ExceptionAsync(async () => await itemRepo.PostItemAsync(invalidPhoneNumber));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
            Assert.Contains(invalidPhoneNumber.PhoneNumber, exception.Message);
        }

        [Fact]
        public async Task PostItemAsync_ValidateGenerated()
        {
            var item = new Item { Name = "Test", PhoneNumber = "780.246.8060" };
            HttpContent outputHttpContent = null;
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>())).Callback<string,HttpContent>((uri,content) => outputHttpContent = content);

            var itemRepo = new ItemRepo(airTableClient.Object);
            await Record.ExceptionAsync(async () => await itemRepo.PostItemAsync(item));

            var outputStr = await outputHttpContent.ReadAsStringAsync();
            var output = JsonConvert.DeserializeObject<AirTablePostItem>(outputStr);

            Assert.NotNull(output);
            Assert.Equal("(780) 246-8060", output.Fields.PhoneNumber);
            Assert.Equal(10, output.Fields.Id.Length);
        }

        [Fact]
        public async Task PostItemAsync_Failure()
        {
            var item = new Item { Name = "Test", PhoneNumber = "780.246.8060" };
            var airTableClient = new Mock<AirTableClientProvider>("");
            airTableClient.Setup(a => a.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

            var itemRepo = new ItemRepo(airTableClient.Object);
            var exception = await Record.ExceptionAsync(async () => await itemRepo.PostItemAsync(item));

            Assert.NotNull(exception);
        }


        #endregion
    }
}
