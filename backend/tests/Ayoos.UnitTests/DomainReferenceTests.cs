using Ayoos.Domain.Common;

namespace Ayoos.UnitTests;

public sealed class DomainReferenceTests
{
    [Fact]
    public void Domain_entity_reference_resolves()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        Assert.Equal(id, entity.Id);
    }

    private sealed class TestEntity(Guid id) : Entity(id);
}
