using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = "";

        [Column("apellido")]
        public string Apellido { get; set; } = "";

        [Column("email")]
        public string Email { get; set; } = "";

        [Column("username")]
        public string Username { get; set; } = "";

        [Column("password")]
        public string Password { get; set; } = "";

        [Column("rol_id")] // ← Cambiado de "rol" a "rol_id"
        public int RolId { get; set; } // ← Ahora es int que referencia a la tabla roles

        [Column("fecha_alta")]
        public DateTimeOffset Fecha_Alta { get; set; }

        // Navigation property
        public Rol Rol { get; set; } = null!;
    }
}