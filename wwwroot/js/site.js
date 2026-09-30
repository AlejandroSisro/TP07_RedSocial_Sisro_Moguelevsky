// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', () => {
    const likeButtons = document.querySelectorAll('[data-like-btn]');
    const commentForms = document.querySelectorAll('[data-comment-form]');
    const btnVerMas = document.getElementById('btnVerMas');

    if (btnVerMas) {
        let offset = document.querySelectorAll('.publication-card').length;

        btnVerMas.addEventListener('click', async () => {
            btnVerMas.disabled = true;
            btnVerMas.textContent = 'Cargando...';

            try {
                const response = await fetch(`/Home/ObtenerPublicaciones?offset=${offset}&cantidad=10`);
                const publicaciones = await response.json();

                if (!publicaciones || publicaciones.length === 0) {
                    btnVerMas.textContent = 'No hay más publicaciones';
                    return;
                }

                const container = document.getElementById('publicaciones-container');
                if (!container) return;

                publicaciones.forEach((publicacion) => {
                    const article = document.createElement('article');
                    article.className = 'publication-card card shadow-sm mb-4';
                    article.innerHTML = `
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <div>
                                    <h5 class="mb-0">${publicacion.titulo}</h5>
                                    <small class="text-muted">${publicacion.nombreUsuario}</small>
                                </div>
                                <small class="text-muted">${new Date(publicacion.fechaPublicacion).toLocaleString()}</small>
                            </div>
                            <img src="${publicacion.imagen}" alt="${publicacion.titulo}" class="publication-image" onerror="this.src='/uploads/default-post.jpg';" />
                            <p class="mt-3 mb-3">${publicacion.descripcion}</p>
                            <div class="d-flex align-items-center gap-2 mb-3">
                                <button type="button" class="${publicacion.tieneLike ? 'btn btn-sm btn-outline-danger' : 'btn btn-sm btn-outline-primary'}" data-like-btn="true" data-publicacion-id="${publicacion.id}">${publicacion.tieneLike ? 'Quitar Me Gusta' : 'Me Gusta'}</button>
                                <span class="likes-count" data-like-count="${publicacion.id}">${publicacion.cantidadLikes} me gusta</span>
                            </div>
                            <div class="comments-box">
                                <h6>Comentarios</h6>
                                <ul class="list-unstyled mb-3" id="comentarios-${publicacion.id}">
                                    ${(publicacion.comentarios || []).map(comentario => `
                                        <li class="comment-item"><strong>${comentario.nombreUsuario}:</strong> <span>${comentario.texto}</span></li>
                                    `).join('') || '<li class="comment-item">Todavía no hay comentarios.</li>'}
                                </ul>
                                <form class="comment-form" data-comment-form="true" data-publicacion-id="${publicacion.id}" method="post">
                                    <div class="input-group">
                                        <textarea name="texto" class="form-control" rows="2" placeholder="Escribí un comentario..."></textarea>
                                        <button type="submit" class="btn btn-primary">Enviar</button>
                                    </div>
                                </form>
                            </div>
                        </div>
                    `;
                    container.appendChild(article);
                });

                offset += publicaciones.length;
                btnVerMas.textContent = 'Ver más';
                btnVerMas.disabled = false;
            } catch (error) {
                console.error(error);
                btnVerMas.textContent = 'Error';
            }
        });
    }

    likeButtons.forEach((button) => {
        button.addEventListener('click', async () => {
            const publicacionId = Number(button.dataset.publicacionId);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

            const response = await fetch('/Home/ToggleLike', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ publicacionId })
            });

            const resultado = await response.json();
            if (!response.ok) {
                alert(resultado.message || 'No se pudo actualizar el like.');
                return;
            }

            const contador = document.querySelector(`[data-like-count="${publicacionId}"]`);
            if (contador) {
                contador.textContent = `${resultado.cantidadLikes} me gusta`;
            }

            button.textContent = resultado.tieneLike ? 'Quitar Me Gusta' : 'Me Gusta';
            button.className = resultado.tieneLike ? 'btn btn-sm btn-outline-danger' : 'btn btn-sm btn-outline-primary';
        });
    });

    commentForms.forEach((form) => {
        form.addEventListener('submit', async (event) => {
            event.preventDefault();

            const publicacionId = Number(form.dataset.publicacionId);
            const textarea = form.querySelector('textarea');
            const texto = textarea.value.trim();

            if (!texto) {
                alert('Escribí un comentario antes de enviar.');
                return;
            }

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
            const response = await fetch('/Home/AgregarComentario', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ publicacionId, texto })
            });

            const resultado = await response.json();
            if (!response.ok) {
                alert(resultado.message || 'No se pudo guardar el comentario.');
                return;
            }

            const lista = document.getElementById(`comentarios-${publicacionId}`);
            if (lista) {
                const item = document.createElement('li');
                item.className = 'comment-item';
                item.innerHTML = `<strong>${resultado.nombreUsuario}:</strong> <span>${resultado.texto}</span>`;
                lista.appendChild(item);
            }

            textarea.value = '';
        });
    });
});
