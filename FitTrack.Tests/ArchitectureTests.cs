using System.Reflection;
using FitTrack.Api.Controllers;
using FitTrack.Application;
using FitTrack.Domain.Entities;
using FitTrack.Infrastructure.Persistence;

namespace FitTrack.Tests;

// The Onion dependency rule: references only ever point inwards.
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(User).Assembly;
    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly Api = typeof(AuthController).Assembly;

    private static List<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

    [Fact]
    public void Domain_depends_on_no_other_project_or_framework_package()
    {
        Assert.DoesNotContain(References(Domain), name =>
            name.StartsWith("FitTrack.") || name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Microsoft.AspNetCore"));
    }

    [Fact]
    public void Application_depends_only_on_Domain_and_knows_nothing_about_EF_or_http()
    {
        var references = References(Application);

        Assert.Contains(Domain.GetName().Name!, references);
        Assert.DoesNotContain(references, name =>
            name is "FitTrack.Infrastructure" or "FitTrack.Api"
            || name.StartsWith("Microsoft.EntityFrameworkCore")
            || name.StartsWith("Npgsql")
            || name.StartsWith("Microsoft.AspNetCore")
            || name.StartsWith("BCrypt"));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        Assert.DoesNotContain("FitTrack.Api", References(Infrastructure));
    }

    [Fact]
    public void Controllers_do_not_touch_persistence_directly()
    {
        var offenders = Api.GetTypes()
            .Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetConstructors().SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType.Assembly == Infrastructure)
                .Select(p => $"{t.Name}({p.ParameterType.Name})"))
            .ToList();

        Assert.Empty(offenders);
    }
}
