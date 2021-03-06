﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;
using Blog.Web.Core;
using Blog.Web.Infrastructure;

namespace Blog.Web.Actions.PostGet
{
    public class PostGetModule : IModule
    {
        public void Execute(IContainer container)
        {
            container.Register(c => new PostGetController(c.Resolve<IMediator>()) {ActionInvoker = c.Resolve<IActionInvoker>()});

            var mediator = container.Resolve<ISubscribeHandlers>();

            mediator.Subscribe<PostRequest, PostGetViewModel>(message =>
            {
                var posts = container.Resolve<IReadOnlyList<PostViewModel>>();
                RefreshContent(container, posts);
                var result = new PostGetViewModel();

                result = new FilteredPostVault(posts).Handle(message, result);
                return result;
            });

            mediator.Subscribe<PostIndexRequest, PostIndexViewModel>(message => new FilteredPostVault(container.Resolve<IReadOnlyList<PostViewModel>>()).Handle(message));
        }

        [Conditional("DEBUG")]
        void RefreshContent(IResolver container, IEnumerable<PostViewModel> posts)
        {
            var storage = container.Resolve<MarkdownSharpContentStorage>();
            foreach (var post in posts)
            {
                storage.Handle(post);
            }
        }
    }

    public class PostGetController : Controller
    {
        private readonly IMediator _mediator;

        public PostGetController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public PostGetViewModel Execute(PostRequest request)
        {
            var model = _mediator.Send<PostRequest, PostGetViewModel>(request);
            if (model.Post == null) return null;
            return model;
        }

        public object Csv(PostRequest request)
        {
            var model = Execute(request);
            if (model == null) return null;
            
            var url = new UrlHelper(HttpContext.Request.RequestContext);
            return new
            {
                model.Post.Title,
                model.Post.PublishedAtCst,
                Url = url.RouteUrl("canonical-slug", new {slug = model.Post.Slug}, "http"),
                model.Post.Content
            };
        }

        public ActionResult Index()
        {
            var model = _mediator.Send<PostIndexRequest, PostIndexViewModel>(new PostIndexRequest());
            return View(model);
        }
    }    

    public class Foo
    {
        public string Hello { get; set; }
    }
    public class PostRequest
    {
        public string Slug { get; set; }
    }

    public class PostGetViewModel
    {
        public PostViewModel Post { get; set; }
        public PostViewModel Previous { get; set; }
        public PostViewModel Next { get; set; }
        
        public bool HasPrevious { get { return Previous != null; } }
        public bool HasNext { get { return Next != null; } }
    }

    public class PostIndexRequest { }

    public class PostIndexViewModel
    {
        public List<PostViewModel> Active { get; set; }
        public int FuturePostCount { get; set; }
    }
}
