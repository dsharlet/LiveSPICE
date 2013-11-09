/* -----------------------------------------------------------------------------------
 * Mini LightBox v1.0 - Compact and Lightweight Lightbox. <http://code.google.com/p/mini-lightbox/>
 * By Jorge Moreno <http://moro.es> <http://alterebro.com>
 * Copyright (c) 2011 Jorge Moreno
 * Licensed under the MIT License: http://www.opensource.org/licenses/mit-license.php
 * -----------------------------------------------------------------------------------	
 */
function listen(ob,ev,fn) {
	if (ob.addEventListener) { ob.addEventListener(ev,fn,false); return true; }
	else if (ob.attachEvent) { ob.attachEvent('on'+ev,fn); return true; }
	else { return false; }
}
function imageloader(url) {
 	this.url = url;
  	this.image = null;
  	this.loadevent = null;
}
imageloader.prototype.load = function() {
	this.image = document.createElement('img');
	var url = this.url;
	var image = this.image;
	var loadevent = this.loadevent;
	listen(this.image, 'load', function(e){ if(loadevent != null){ loadevent(url, image); }}, false);
	this.image.src = this.url;	
}
imageloader.prototype.getimage = function() {
	return this.image;
}
function mini_lightbox_close() {
	document.getElementById('minilightbox').style.display = 'none';
	document.getElementsByTagName('body')[0].removeChild(minilightbox);
}
function mini_lightbox(image_path, text) {
	minilightbox = document.createElement('div');
	minilightbox.setAttribute('id', 'minilightbox');
	mlbx_bg = document.createElement('div');
	mlbx_bg.setAttribute('id', 'minilightbox-background');
	mlbx_bg.onclick = function() { mini_lightbox_close(); }
	mlbx_cnt = document.createElement('div');
	mlbx_cnt.setAttribute('id', 'minilightbox-content');
		mlbx_img = document.createElement('img');
		mlbx_img.setAttribute('id', 'minilightbox-image');
		mlbx_img.setAttribute('src', image_path);
		
		mlbx_cls = document.createElement('div');
		mlbx_cls.setAttribute('id', 'minilightbox-closebutton');
		mlbx_cls.innerHTML = '<a href="javascript:mini_lightbox_close();" title="close">&times;</a>';
		
		if (typeof(text) != 'undefined') {
			mlbx_txt = document.createElement('div');
			mlbx_txt.setAttribute('id', 'minilightbox-text');
			mlbx_txt.innerHTML = text;
		}
		
	document.getElementsByTagName('body')[0].appendChild(minilightbox);
	minilightbox.appendChild(mlbx_bg);
	minilightbox.appendChild(mlbx_cnt);
	mlbx_cnt.appendChild(mlbx_img);
	mlbx_cnt.appendChild(mlbx_cls);
	if (typeof(text) != 'undefined') { mlbx_cnt.appendChild(mlbx_txt); }
	
	var loader = new imageloader(image_path);
	loader.loadevent = function(url, image){		
		mlbx_cnt.style.left = '50%';
		mlbx_cnt.style.marginLeft = '-'+Math.round((image.width+20)/2) + 'px';
		mlbx_cnt.style.top = '50%';
		mlbx_cnt.style.visibility = 'hidden';
		mlbx_cnt.style.display = 'block';
		mlbx_cnt.style.marginTop = '-'+Math.round(mlbx_cnt.offsetHeight/2) + 'px';
		mlbx_cnt.style.width = image.width+mlbx_cnt.style.marginLeft*2 + 'px';
		mlbx_bg.style.backgroundImage = 'none';
		mlbx_cnt.style.visibility = 'visible';
	};
	loader.load();
}
function get_lightboxed() {
	var l = document.getElementsByTagName('a');
	for (var i=0; i<l.length; i++) {
		if (l[i].getAttribute("rel") == "mini-lightbox") {
			l[i].onclick = function() {
				mini_lightbox(this.href, this.title);
				return false;
			}
		}
	}
}
listen(window,'load',get_lightboxed);