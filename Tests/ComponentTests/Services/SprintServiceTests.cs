using backend.Application.Exceptions;
using backend.Application.Interfaces;
using backend.Application.Services;
using ComponentTests.Helpers;
using Moq;

namespace ComponentTests.Services;

[TestFixture]
public class SprintServiceTests
{
    private Mock<ISprintRepository> _sprintRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IUserContext> _user = null!;
    private SprintService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sprintRepo = new Mock<ISprintRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _access = new Mock<IBoardAccessService>();
        _user = new Mock<IUserContext>();
        _user.Setup(u => u.GetUserId()).Returns(1);

        _sut = new SprintService(
            _sprintRepo.Object,
            _boardRepo.Object,
            _access.Object,
            _user.Object,
            TestDataFactory.EmptyEvents());
    }

    [Test]
    public async Task Create_ValidRequest_AddsSprint()
    {
        var request = TestDataFactory.CreateSprintRequest();
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _sprintRepo.Setup(r => r.Add(It.IsAny<Sprint>()))
            .ReturnsAsync((Sprint s) =>
            {
                s.Id = 4;
                return s;
            });

        var result = await _sut.Create(request);

        Assert.That(result.Id, Is.EqualTo(4));
        Assert.That(result.Name, Is.EqualTo("Sprint 1"));
        Assert.That(result.BoardId, Is.EqualTo(1));
        _sprintRepo.Verify(r => r.Add(It.IsAny<Sprint>()), Times.Once);
    }

    [Test]
    public void Create_MissingName_ThrowsValidation()
    {
        var request = TestDataFactory.CreateSprintRequest(name: " ");

        var ex = Assert.ThrowsAsync<ValidationException>(() => _sut.Create(request));

        Assert.That(ex!.Message, Does.Contain("required"));
        _sprintRepo.Verify(r => r.Add(It.IsAny<Sprint>()), Times.Never);
    }

    [Test]
    public void Create_BoardMissing_ThrowsNotFound()
    {
        var request = TestDataFactory.CreateSprintRequest();
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync((Board?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _sut.Create(request));

        Assert.That(ex!.Message, Does.Contain("Board does not exist"));
    }

    [Test]
    public async Task GetById_Existing_ReturnsSprint()
    {
        _sprintRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateSprint());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());

        var result = await _sut.GetById(1);

        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Sprint 1"));
    }

    [Test]
    public void GetById_Missing_ThrowsNotFound()
    {
        _sprintRepo.Setup(r => r.GetById(999)).ReturnsAsync((Sprint?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.GetById(999));
    }

    [Test]
    public void GetById_NoAccess_PropagatesForbidden()
    {
        _sprintRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateSprint());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ThrowsAsync(new ForbiddenException("You do not have access to this board."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetById(1));

        Assert.That(ex!.Message, Does.Contain("do not have access"));
    }

    [Test]
    public void Update_InvalidDates_ThrowsValidation()
    {
        var request = TestDataFactory.CreateUpdateSprintRequest();
        request.StartDate = DateTime.UtcNow.Date.AddDays(10);
        request.EndDate = DateTime.UtcNow.Date;
        _sprintRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateSprint());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());

        var ex = Assert.ThrowsAsync<ValidationException>(() => _sut.Update(1, request));

        Assert.That(ex!.Message, Does.Contain("End date"));
    }

    [Test]
    public async Task Delete_Existing_RemovesSprint()
    {
        var sprint = TestDataFactory.CreateSprint();
        _sprintRepo.Setup(r => r.GetById(1)).ReturnsAsync(sprint);
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());

        await _sut.Delete(1);

        _sprintRepo.Verify(r => r.Delete(sprint), Times.Once);
    }

    [Test]
    public void Delete_Missing_ThrowsNotFound()
    {
        _sprintRepo.Setup(r => r.GetById(999)).ReturnsAsync((Sprint?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.Delete(999));
    }

    [Test]
    public void Delete_NoAccess_PropagatesForbidden()
    {
        _sprintRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateSprint());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ThrowsAsync(new ForbiddenException("You do not have access to this board."));

        Assert.ThrowsAsync<ForbiddenException>(() => _sut.Delete(1));
    }
}
