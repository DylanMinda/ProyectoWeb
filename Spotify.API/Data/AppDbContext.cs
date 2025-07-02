using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Spotify.Modelos;

    public class AppDbContext : DbContext
    {
        public AppDbContext (DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Spotify.Modelos.Usuario> Usuarios { get; set; } = default!;

public DbSet<Spotify.Modelos.Album> Albums { get; set; } = default!;

public DbSet<Spotify.Modelos.Cancion> Canciones { get; set; } = default!;

public DbSet<Spotify.Modelos.Plan> Planes { get; set; } = default!;

public DbSet<Spotify.Modelos.Playlist> Playlists { get; set; } = default!;
    }
