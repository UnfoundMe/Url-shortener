using System.Reflection;
using DomainEntities = global::UrlShortener.Domain.Entities;
using ApplicationAbstractions = global::UrlShortener.Application.Abstractions;
using ApplicationServices = global::UrlShortener.Application.Services;
using InfrastructureCaching = global::UrlShortener.Infrastructure.Caching;
using InfrastructurePersistence = global::UrlShortener.Infrastructure.Persistence;
using InfrastructureShortCodes = global::UrlShortener.Infrastructure.ShortCodes;

namespace UrlShortener.UnitTests.Architecture;

/// <summary>
/// Verifies the clean-architecture dependency direction: API -&gt; Application -&gt; Domain,
/// with Infrastructure implementing Application/Domain abstractions rather than the other way
/// around. Uses plain reflection over referenced assemblies so no extra test dependency is needed.
/// </summary>
public class LayerDependencyTests
{
    private static Assembly DomainAssembly => typeof(DomainEntities.ShortenedUrl).Assembly;
    private static Assembly ApplicationAssembly => typeof(ApplicationServices.IUrlShortenerService).Assembly;
    private static Assembly InfrastructureAssembly => typeof(InfrastructurePersistence.UrlShortenerDbContext).Assembly;

    private static IEnumerable<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    [Fact]
    public void Domain_DoesNotReferenceApplication()
    {
        Assert.DoesNotContain("UrlShortener.Application", ReferencedAssemblyNames(DomainAssembly));
    }

    [Fact]
    public void Domain_DoesNotReferenceInfrastructure()
    {
        Assert.DoesNotContain("UrlShortener.Infrastructure", ReferencedAssemblyNames(DomainAssembly));
    }

    [Fact]
    public void Domain_DoesNotReferenceAspNetCoreOrEntityFrameworkCore()
    {
        var referenced = ReferencedAssemblyNames(DomainAssembly).ToList();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.Equals("Npgsql", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("StackExchange.Redis", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        Assert.DoesNotContain("UrlShortener.Infrastructure", ReferencedAssemblyNames(ApplicationAssembly));
    }

    [Fact]
    public void Application_DoesNotReferenceEntityFrameworkCoreOrNpgsqlOrRedis()
    {
        var referenced = ReferencedAssemblyNames(ApplicationAssembly).ToList();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.Equals("Npgsql", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("StackExchange.Redis", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_DoesNotReferenceAspNetCore()
    {
        var referenced = ReferencedAssemblyNames(ApplicationAssembly).ToList();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_ReferencesApplicationAndDomain()
    {
        var referenced = ReferencedAssemblyNames(InfrastructureAssembly).ToList();

        Assert.Contains("UrlShortener.Application", referenced);
        Assert.Contains("UrlShortener.Domain", referenced);
    }

    [Fact]
    public void Infrastructure_ImplementsApplicationAbstractions()
    {
        Assert.True(typeof(ApplicationAbstractions.IShortenedUrlRepository)
            .IsAssignableFrom(typeof(InfrastructurePersistence.EfShortenedUrlRepository)));
        Assert.True(typeof(ApplicationAbstractions.IShortUrlCache)
            .IsAssignableFrom(typeof(InfrastructureCaching.RedisShortUrlCache)));
        Assert.True(typeof(ApplicationAbstractions.IShortCodeGenerator)
            .IsAssignableFrom(typeof(InfrastructureShortCodes.Base62ShortCodeGenerator)));
    }
}
