using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.ListDepartments;

/// <summary>
/// <c>GET /staff/departments</c>. Every department in page order, with how many active members
/// are in each, including the empty ones so a form can offer all of them. Extension, § 10.
/// </summary>
public sealed class ListDepartmentsHandler(IStaffRepository staff)
{
    public async Task<IReadOnlyList<DepartmentDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var counts = await staff.DepartmentCountsAsync(cancellationToken);

        return
        [
            .. Departments.All.Select(department =>
                new DepartmentDto(department.Code, department.Name, counts.GetValueOrDefault(department.Code))),
        ];
    }
}
