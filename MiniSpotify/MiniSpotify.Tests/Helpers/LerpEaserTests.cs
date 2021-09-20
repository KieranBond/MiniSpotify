using FluentAssertions;
using MiniSpotify.HelperScripts;
using NUnit.Framework;
using static MiniSpotify.HelperScripts.LerpEaser;

namespace MiniSpotify.Helpers.Tests
{
    public class LerpEaserTests
    {
        [Theory]
        [TestCase( 0, 1, 0 )]
        [TestCase( 0.5f, 1, 0.5f )]
        [TestCase( 1, 1, 1 )]
        public void Linear_Returns_Expected( float currentLerpTime, float duration, float expected )
        {
            var result = LerpEaser.GetLerpT( EaseType.Linear, currentLerpTime, duration );
            result.Should().Be( expected );
        }
    }
}