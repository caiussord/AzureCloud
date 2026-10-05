const api = window.APP_CONFIG.apiBaseUrl.replace(/\/$/, "");
const form = document.querySelector("#product-form"), list = document.querySelector("#list"), status = document.querySelector("#status");
const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

async function request(path, options = {}) {
  const response = await fetch(`${api}${path}`, { headers: { "Content-Type": "application/json" }, ...options });
  if (!response.ok && response.status !== 204) { const body = await response.json().catch(() => ({})); throw new Error(body.error || "Não foi possível concluir a operação."); }
  return response.status === 204 ? null : response.json();
}
async function load() {
  status.textContent = "Carregando produtos…"; list.innerHTML = "";
  try { const products = await request("/api/products/"); const total = document.querySelector("#total"); if (total) total.textContent = products.length; status.textContent = products.length ? "" : "Ainda não há produtos cadastrados."; products.forEach(render); }
  catch { status.textContent = "Não foi possível conectar à API. Confira frontend/config.js e inicie a API."; }
}
function render(product) {
  const element = document.createElement("article"); element.className = "product";
  element.innerHTML = `<div><h3>${escapeHtml(product.name)}</h3><small>${escapeHtml(product.category)} · ${product.stock} em estoque</small></div><div class="product-side"><span class="product-price">${money.format(product.price)}</span><div class="product-actions"><button data-edit="${product.id}">Editar</button><button class="delete" data-delete="${product.id}">Excluir</button></div></div>`;
  element.querySelector("[data-edit]").onclick = () => edit(product); element.querySelector("[data-delete]").onclick = () => remove(product.id, product.name); list.append(element);
}
function edit(p) { document.querySelector("#id").value=p.id; document.querySelector("#name").value=p.name; document.querySelector("#category").value=p.category; document.querySelector("#price").value=p.price; document.querySelector("#stock").value=p.stock; document.querySelector("#description").value=p.description||""; document.querySelector("#form-title").textContent="Editar produto"; document.querySelector("#cancel").hidden=false; window.scrollTo({top:0,behavior:"smooth"}); }
function reset() { form.reset(); document.querySelector("#id").value=""; document.querySelector("#form-title").textContent="Novo produto"; document.querySelector("#cancel").hidden=true; }
async function remove(id,name) { if (!confirm(`Excluir “${name}”?`)) return; try { await request(`/api/products/${id}`,{method:"DELETE"}); load(); } catch(e) { alert(e.message); } }
form.onsubmit = async event => { event.preventDefault(); const id=document.querySelector("#id").value; const data={name:document.querySelector("#name").value.trim(),category:document.querySelector("#category").value.trim(),price:Number(document.querySelector("#price").value),stock:Number(document.querySelector("#stock").value),description:document.querySelector("#description").value.trim()||null}; try { await request(id?`/api/products/${id}`:"/api/products/",{method:id?"PUT":"POST",body:JSON.stringify(data)}); reset(); load(); } catch(e) { alert(e.message); } };
document.querySelector("#cancel").onclick=reset; document.querySelector("#refresh").onclick=load;
function escapeHtml(value) { const d=document.createElement("div"); d.textContent=value; return d.innerHTML; } load();
