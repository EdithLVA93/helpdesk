using HelpDesk.Application.DTOs.Users;
using HelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

/// <summary>
/// Gestión de usuarios. El controlador solo traduce HTTP ↔ servicio: la
/// validación y las reglas viven en IUserService, y los errores los convierte
/// GlobalExceptionHandler en respuestas ProblemDetails.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Crea un usuario.</summary>
    /// <response code="201">Usuario creado. La cabecera Location apunta a GET /api/users/{id}.</response>
    /// <response code="400">Datos inválidos: nombre vacío o correo con formato incorrecto.</response>
    /// <response code="409">Ya existe un usuario con ese correo.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Lista todos los usuarios, ordenados por nombre.</summary>
    /// <response code="200">Lista de usuarios (vacía si no hay ninguno).</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await userService.GetAllAsync(cancellationToken));

    /// <summary>Obtiene un usuario por su id.</summary>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">No existe un usuario con ese id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await userService.GetByIdAsync(id, cancellationToken));
}
