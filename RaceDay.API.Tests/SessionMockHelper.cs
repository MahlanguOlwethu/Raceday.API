using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace RaceDay.API.Tests
{
    public static class SessionMockHelper
    {
        public static void SetSessionData(ControllerBase controller, string userId, string role)
        {
            var mockSession = new Mock<ISession>();

            mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    if (key == "UserId" && userId != null)
                    {
                        value = Encoding.UTF8.GetBytes(userId);
                        return true;
                    }
                    if (key == "UserRole" && role != null)
                    {
                        value = Encoding.UTF8.GetBytes(role);
                        return true;
                    }
                    value = null;
                    return false;
                });

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext.Object
            };
        }
    }
}