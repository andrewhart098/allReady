﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AllReady.Areas.Admin.Controllers;
using AllReady.Models;
using AllReady.UnitTest.Extensions;
using MediatR;
using Microsoft.AspNet.Mvc;
using Moq;
using Xunit;
using System.Linq;
using AllReady.Areas.Admin.Features.Events;
using Microsoft.AspNet.Authorization;
using AllReady.Areas.Admin.Features.Itineraries;
using AllReady.Areas.Admin.Models;
using AllReady.Areas.Admin.Models.ItineraryModels;
using AllReady.Areas.Admin.Models.Validators;

namespace AllReady.UnitTest.Areas.Admin.Controllers
{
    public class ItineraryAdminControllerTests
    {
        [Fact]
        public void ControllerHasAreaAtttributeWithTheCorrectAreaName()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var attribute = sut.GetAttributes().OfType<AreaAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.RouteValue, "Admin");
        }

        [Fact]
        public void ControllerHasAreaAuthorizeAttributeWithCorrectPolicy()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var attribute = sut.GetAttributes().OfType<AuthorizeAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Policy, "OrgAdmin");
        }

        [Fact]
        public void DetailsHasHttpGetAttribute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var attribute = sut.GetAttributesOn(x => x.Details(It.IsAny<int>())).OfType<HttpGetAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
        }

        [Fact]
        public void DetailsHasRouteAttributeWithCorrectRoute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var routeAttribute = sut.GetAttributesOn(x => x.Details(It.IsAny<int>())).OfType<RouteAttribute>().SingleOrDefault();
            Assert.NotNull(routeAttribute);
            Assert.Equal(routeAttribute.Template, "Admin/Itinerary/Details/{id}");
        }

        [Fact]
        public async Task DetailsSendsEventDetailQueryAsyncWithCorrectEventId()
        {
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(mock => mock.SendAsync(It.IsAny<ItineraryDetailQuery>())).ReturnsAsync(null).Verifiable();

            var sut = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);
            await sut.Details(1);

            mockMediator.Verify(x => x.SendAsync(It.IsAny<ItineraryDetailQuery>()), Times.Once);
        }

        [Fact]
        public async Task DetailsReturnsHttpNotFoundResultWhenEventIsNull()
        {
            ItineraryController controller;
            MockMediatorItineraryDetailQuery(out controller);
            Assert.IsType<HttpNotFoundResult>(await controller.Details(It.IsAny<int>()));
        }

        [Fact]
        public async Task DetailsReturnsHttpUnauthorizedResultWhenUserIsNotOrgAdmin()
        {
            var sut = GetItineraryControllerWithDetailsQuery(UserType.OrgAdmin.ToString(), It.IsAny<int>());
            Assert.IsType<HttpUnauthorizedResult>(await sut.Details(It.IsAny<int>()));
        }

        [Fact]
        public async Task DetailsReturnsCorrectViewModelWhenEventIsNotNullAndUserIsOrgAdmin()
        {
            var sut = GetItineraryControllerWithDetailsQuery(UserType.OrgAdmin.ToString(), 1);
            Assert.IsType<ViewResult>(await sut.Details(It.IsAny<int>()));
        }

        [Fact]
        public async Task DetailsReturnsCorrectViewModelWhenEventIsNotNullAndUserIsSiteAdmin()
        {
            var sut = GetItineraryControllerWithDetailsQuery(UserType.SiteAdmin.ToString(), 0);
            Assert.IsType<ViewResult>(await sut.Details(It.IsAny<int>()));
        }
        
        [Fact]
        public void CreateHasHttpPostAttribute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var attribute = sut.GetAttributesOn(x => x.Create(It.IsAny<ItineraryEditModel>())).OfType<HttpPostAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
        }

        [Fact]
        public void CreateHasRouteAttributeWithCorrectRoute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var routeAttribute = sut.GetAttributesOn(x => x.Create(It.IsAny<ItineraryEditModel>())).OfType<RouteAttribute>().SingleOrDefault();
            Assert.NotNull(routeAttribute);
            Assert.Equal(routeAttribute.Template, "Admin/Itinerary/Create");
        }

        [Fact]
        public void CreateHasValidateAntiForgeryAttribute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var routeAttribute = sut.GetAttributesOn(x => x.Create(It.IsAny<ItineraryEditModel>())).OfType<ValidateAntiForgeryTokenAttribute>().SingleOrDefault();
            Assert.NotNull(routeAttribute);
        }

        [Fact]
        public async Task CreateReturnsHttpBadRequestWhenModelIsNull()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            Assert.IsType<BadRequestResult>(await sut.Create(null).ConfigureAwait(false));
        }

        [Fact]
        public async Task CreateSendsEventSummaryQuery()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>()))
                .ReturnsAsync(It.IsAny<EventSummaryModel>()).Verifiable();

            var sut = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);
            await sut.Create(model);

            mockMediator.Verify(x => x.SendAsync(It.IsAny<EventSummaryQuery>()), Times.Once);
        }

        [Fact]
        public async Task CreateReturnsHttpBadRequestIfEventNull()
        {
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>())).ReturnsAsync(null);

            var sut = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);
            Assert.IsType<BadRequestResult>(await sut.Create(It.IsAny<ItineraryEditModel>()).ConfigureAwait(false));
        }

        [Fact]
        public async Task CreateReturnsHttpUnauthorizedResultWhenUserIsNotOrgAdminForEventOrg()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var sut = GetItineraryControllerWithEventSummaryQuery(UserType.OrgAdmin.ToString(), 5);
            Assert.IsType<HttpUnauthorizedResult>(await sut.Create(model));
        }

        [Fact]
        public async Task CreateReturnsOkResultWhenUserIsOrgAdmin()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var sut = GetItineraryControllerWithEventSummaryQuery(UserType.OrgAdmin.ToString(), 1);
            Assert.IsType<HttpOkObjectResult>(await sut.Create(model));
        }

        [Fact]
        public async Task CreateReturnsOkResultWhenUserIsSiteAdmin_AndModelIsValid_AndSuccessfulAdd()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var sut = GetItineraryControllerWithEventSummaryQuery(UserType.SiteAdmin.ToString(), 0);
            Assert.IsType<HttpOkObjectResult>(await sut.Create(model));
        }

        [Fact]
        public async Task CreateReturnsHttpBadRequestResultWhenModelStateHasError()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Date = new DateTime(2016, 06, 01)
            };

            var sut = GetItineraryControllerWithEventSummaryQuery(UserType.SiteAdmin.ToString(), 0);

            sut.ModelState.AddModelError("key","Error");

            var result = await sut.Create(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCallsValidator()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var mediator = new Mock<IMediator>();

            mediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>())).ReturnsAsync(new EventSummaryModel { Id = 1, Name = "Event", OrganizationId = 1, StartDateTime = new DateTimeOffset(new DateTime(2016, 01, 01)), EndDateTime = new DateTimeOffset(new DateTime(2016, 12, 31)) });
            mediator.Setup(x => x.SendAsync(It.IsAny<EditItineraryCommand>())).ReturnsAsync(1);

            var mockValidator = new Mock<IItineraryEditModelValidator>();
            mockValidator.Setup(mock => mock.Validate(It.IsAny<ItineraryEditModel>(), It.IsAny<EventSummaryModel>())).Returns(new List<KeyValuePair<string, string>>()).Verifiable();

            var sut = new ItineraryController(mediator.Object, mockValidator.Object);
            sut.SetClaims(new List<Claim>
            {
                new Claim(AllReady.Security.ClaimTypes.UserType, UserType.SiteAdmin.ToString()),
                new Claim(AllReady.Security.ClaimTypes.Organization, "1")
            });

            await sut.Create(model);

            mockValidator.Verify(x => x.Validate(It.IsAny<ItineraryEditModel>(), It.IsAny<EventSummaryModel>()), Times.Once);
        }

        [Fact]
        public async Task CreateReturnsHttpBadRequestResultWhenValidatonFails()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var mediator = new Mock<IMediator>();

            mediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>())).ReturnsAsync(new EventSummaryModel { Id = 1, Name = "Event", OrganizationId = 1, StartDateTime = new DateTimeOffset(new DateTime(2016, 01, 01)), EndDateTime = new DateTimeOffset(new DateTime(2016, 12, 31)) });
            mediator.Setup(x => x.SendAsync(It.IsAny<EditItineraryCommand>())).ReturnsAsync(1);

            var validatorError = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };

            var mockValidator = new Mock<IItineraryEditModelValidator>();
            mockValidator.Setup(mock => mock.Validate(It.IsAny<ItineraryEditModel>(), It.IsAny<EventSummaryModel>())).Returns(validatorError).Verifiable();

            var sut = new ItineraryController(mediator.Object, mockValidator.Object);
            sut.SetClaims(new List<Claim>
            {
                new Claim(AllReady.Security.ClaimTypes.UserType, UserType.SiteAdmin.ToString()),
                new Claim(AllReady.Security.ClaimTypes.Organization, "1")
            });

            var result = await sut.Create(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCallsEditCommand()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var mediator = new Mock<IMediator>();

            mediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>())).ReturnsAsync(new EventSummaryModel { Id = 1, Name = "Event", OrganizationId = 1, StartDateTime = new DateTimeOffset(new DateTime(2016, 01, 01)), EndDateTime = new DateTimeOffset(new DateTime(2016, 12, 31)) });
            mediator.Setup(x => x.SendAsync(It.IsAny<EditItineraryCommand>())).ReturnsAsync(0).Verifiable();

            var mockValidator = new Mock<IItineraryEditModelValidator>();
            mockValidator.Setup(mock => mock.Validate(It.IsAny<ItineraryEditModel>(), It.IsAny<EventSummaryModel>())).Returns(new List<KeyValuePair<string, string>>()).Verifiable();

            var sut = new ItineraryController(mediator.Object, mockValidator.Object);
            sut.SetClaims(new List<Claim>
            {
                new Claim(AllReady.Security.ClaimTypes.UserType, UserType.SiteAdmin.ToString()),
                new Claim(AllReady.Security.ClaimTypes.Organization, "1")
            });

            var result = await sut.Create(model);

            mediator.Verify(x => x.SendAsync(It.IsAny<EditItineraryCommand>()), Times.Once);
        }

        [Fact]
        public async Task CreateReturnsHttpBadRequestResultWhenEditItineraryReturnsZero()
        {
            var model = new ItineraryEditModel
            {
                EventId = 1,
                Name = "Test",
                Date = new DateTime(2016, 06, 01)
            };

            var mediator = new Mock<IMediator>();

            mediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>())).ReturnsAsync(new EventSummaryModel { Id = 1, Name = "Event", OrganizationId = 1, StartDateTime = new DateTimeOffset(new DateTime(2016, 01, 01)), EndDateTime = new DateTimeOffset(new DateTime(2016, 12, 31)) });
            mediator.Setup(x => x.SendAsync(It.IsAny<EditItineraryCommand>())).ReturnsAsync(0);

            var mockValidator = new Mock<IItineraryEditModelValidator>();
            mockValidator.Setup(mock => mock.Validate(It.IsAny<ItineraryEditModel>(), It.IsAny<EventSummaryModel>())).Returns(new List<KeyValuePair<string, string>>()).Verifiable();

            var sut = new ItineraryController(mediator.Object, mockValidator.Object);
            sut.SetClaims(new List<Claim>
            {
                new Claim(AllReady.Security.ClaimTypes.UserType, UserType.SiteAdmin.ToString()),
                new Claim(AllReady.Security.ClaimTypes.Organization, "1")
            });

            var result = await sut.Create(model);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public void AddTeamMemberHasHttpPostAttribute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var attribute = sut.GetAttributesOn(x => x.AddTeamMember(It.IsAny<int>(), It.IsAny<int>())).OfType<HttpPostAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
        }

        [Fact]
        public void AddTeamMemberHasRouteAttributeWithCorrectRoute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var routeAttribute = sut.GetAttributesOn(x => x.AddTeamMember(It.IsAny<int>(), It.IsAny<int>())).OfType<RouteAttribute>().SingleOrDefault();
            Assert.NotNull(routeAttribute);
            Assert.Equal(routeAttribute.Template, "Admin/Itinerary/AddTeamMember");
        }

        [Fact]
        public void AddTeamMemberHasValidateAntiForgeryAttribute()
        {
            var sut = new ItineraryController(Mock.Of<IMediator>(), MockSuccessValidation().Object);
            var routeAttribute = sut.GetAttributesOn(x => x.AddTeamMember(It.IsAny<int>(), It.IsAny<int>())).OfType<ValidateAntiForgeryTokenAttribute>().SingleOrDefault();
            Assert.NotNull(routeAttribute);
        }

        [Fact]
        public async Task AddTeamMemberCallsAddTeamMemberCommand()
        {
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.SendAsync(It.IsAny<AddTeamMemberCommand>())).ReturnsAsync(true);

            var sut = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);
            await sut.AddTeamMember(1, 1);

            mockMediator.Verify(x => x.SendAsync(It.IsAny<AddTeamMemberCommand>()), Times.Once);
        }

        [Fact]
        public async Task AddTeamMemberDoesNotCallAddTeamMemberCommand_WhenIdIsZero()
        {
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.SendAsync(It.IsAny<AddTeamMemberCommand>())).ReturnsAsync(true);

            var sut = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);
            await sut.AddTeamMember(0, 1);

            mockMediator.Verify(x => x.SendAsync(It.IsAny<AddTeamMemberCommand>()), Times.Never);
        }

        [Fact]
        public async Task AddTeamMemberDoesNotCallAddTeamMemberCommand_WhenSelectedTeamMemberIsZero()
        {
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.SendAsync(It.IsAny<AddTeamMemberCommand>())).ReturnsAsync(true);

            var sut = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);
            await sut.AddTeamMember(1, 0);

            mockMediator.Verify(x => x.SendAsync(It.IsAny<AddTeamMemberCommand>()), Times.Never);
        }


        #region Helper Methods

        private static Mock<IMediator> MockMediatorItineraryDetailQuery(out ItineraryController controller)
        {
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(mock => mock.SendAsync(It.IsAny<ItineraryDetailQuery>())).ReturnsAsync(null).Verifiable();

            controller = new ItineraryController(mockMediator.Object, MockSuccessValidation().Object);

            return mockMediator;
        }

        private static Mock<IItineraryEditModelValidator> MockSuccessValidation()
        {
            var mockValidator = new Mock<IItineraryEditModelValidator>();

            mockValidator.Setup(mock => mock.Validate(It.IsAny<ItineraryEditModel>(), It.IsAny<EventSummaryModel>())).Returns(new List<KeyValuePair<string, string>>()).Verifiable();

           return mockValidator;
        }

        private static ItineraryController GetItineraryControllerWithDetailsQuery(string userType, int organizationId)
        {
            var mediator = new Mock<IMediator>();

            mediator.Setup(x => x.SendAsync(It.IsAny<ItineraryDetailQuery>())).ReturnsAsync(new ItineraryDetailsModel { Id = 1, Name = "Itinerary", EventId = 1, EventName = "Event Name", OrganizationId = 1, Date = new DateTime(2016, 07, 01) });

            var sut = new ItineraryController(mediator.Object, MockSuccessValidation().Object);
            sut.SetClaims(new List<Claim>
            {
                new Claim(AllReady.Security.ClaimTypes.UserType, userType),
                new Claim(AllReady.Security.ClaimTypes.Organization, organizationId.ToString())
            });

            return sut;
        }

        private static ItineraryController GetItineraryControllerWithEventSummaryQuery(string userType, int organizationId)
        {
            var mediator = new Mock<IMediator>();

            mediator.Setup(x => x.SendAsync(It.IsAny<EventSummaryQuery>())).ReturnsAsync(new EventSummaryModel { Id = 1, Name = "Event", OrganizationId = 1, StartDateTime = new DateTimeOffset(new DateTime(2016, 01, 01)), EndDateTime = new DateTimeOffset(new DateTime(2016, 12, 31)) });
            mediator.Setup(x => x.SendAsync(It.IsAny<EditItineraryCommand>())).ReturnsAsync(1);

            var sut = new ItineraryController(mediator.Object, MockSuccessValidation().Object);
            sut.SetClaims(new List<Claim>
            {
                new Claim(AllReady.Security.ClaimTypes.UserType, userType),
                new Claim(AllReady.Security.ClaimTypes.Organization, organizationId.ToString())
            });

            return sut;
        }

        #endregion
    }
}