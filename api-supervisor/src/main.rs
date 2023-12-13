#[macro_use] extern crate rocket;

mod db;
mod models;
mod routes;

#[launch]
fn rocket() -> _ {
    rocket::build()
    .attach(db::init())
    .mount("/scopes", routes::scopes::get_routes())
}