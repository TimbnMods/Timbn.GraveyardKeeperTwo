namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnExpressionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void AnEmptyExpressionIsRefused(string text) =>
        Assert.Throws<ArgumentException>(() => new TimbnExpression(text));

    [Fact]
    public void TheTextIsKeptAsWritten()
    {
        var expression = new TimbnExpression("$money >= 500");

        Assert.Equal("$money >= 500", expression.Text);
    }

    [Fact]
    public void OutsideTheGameNothingIsCompiledUntilItRuns()
    {
        Assert.False(TimbnExpression.CompileNow);

        var expression = new TimbnExpression("this is not (valid");

        Assert.Equal("this is not (valid", expression.Text);
    }
}
